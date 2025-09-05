using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ProjectIgnite.Utilities
{
    /// <summary>
    /// 重试机制辅助类
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// 执行带重试的异步操作
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="delayMs">重试间隔（毫秒）</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <returns>操作结果</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            int maxRetries = 3,
            int delayMs = 1000,
            ILogger? logger = null,
            string operationName = "Database Operation")
        {
            Exception? lastException = null;
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (IsRetriableException(ex))
                {
                    lastException = ex;
                    
                    if (attempt == maxRetries)
                    {
                        logger?.LogError(ex, "{OperationName} failed after {MaxRetries} attempts", 
                            operationName, maxRetries + 1);
                        break;
                    }
                    
                    logger?.LogWarning(ex, "{OperationName} failed on attempt {Attempt}, retrying in {Delay}ms", 
                        operationName, attempt + 1, delayMs);
                    
                    await Task.Delay(delayMs * (attempt + 1)); // 指数退避
                }
                catch (Exception ex)
                {
                    // 不可重试的异常直接抛出
                    logger?.LogError(ex, "{OperationName} failed with non-retriable exception", operationName);
                    throw;
                }
            }
            
            throw lastException ?? new InvalidOperationException($"{operationName} failed after {maxRetries + 1} attempts");
        }
        
        /// <summary>
        /// 执行带重试的异步操作（无返回值）
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="delayMs">重试间隔（毫秒）</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        public static async Task ExecuteWithRetryAsync(
            Func<Task> operation,
            int maxRetries = 3,
            int delayMs = 1000,
            ILogger? logger = null,
            string operationName = "Database Operation")
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await operation();
                return true;
            }, maxRetries, delayMs, logger, operationName);
        }
        
        /// <summary>
        /// 判断异常是否可重试
        /// </summary>
        /// <param name="exception">异常</param>
        /// <returns>是否可重试</returns>
        private static bool IsRetriableException(Exception exception)
        {
            return exception switch
            {
                // 数据库连接相关异常
                Microsoft.Data.Sqlite.SqliteException => true,
                System.Data.Common.DbException => true,
                
                // 网络相关异常
                System.Net.NetworkInformation.NetworkInformationException => true,
                System.Net.Sockets.SocketException => true,
                
                // 超时异常
                TimeoutException => true,
                TaskCanceledException => true,
                
                // IO异常（可能是临时的）
                System.IO.IOException => true,
                
                // 参数异常不重试（注意顺序：子类在前，父类在后）
                ArgumentNullException => false,
                ArgumentException => false,
                
                // 其他异常默认不重试
                _ => false
            };
        }
    }
}