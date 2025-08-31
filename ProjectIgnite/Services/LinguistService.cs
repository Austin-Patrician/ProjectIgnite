using ProjectIgnite.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectIgnite.Services
{
    /// <summary>
    /// Linguist语言分析服务实现
    /// </summary>
    public class LinguistService : ILinguistService
    {
        private readonly string? _linguistPath;
        private readonly Dictionary<string, string> _languageColors;

        public LinguistService()
        {
            _linguistPath = FindLinguistExecutable();
            _languageColors = InitializeLanguageColors();
        }

        /// <summary>
        /// 分析项目目录的语言组成
        /// </summary>
        public async Task<LinguistAnalysisResult> AnalyzeProjectAsync(
            string projectPath,
            IProgress<LinguistAnalysisResult>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new LinguistAnalysisResult
            {
                ProjectPath = projectPath,
                Status = LinguistAnalysisStatus.Pending,
                StartTime = DateTime.Now
            };

            try
            {
                // 检查项目路径是否存在
                if (!Directory.Exists(projectPath))
                {
                    result.Status = LinguistAnalysisStatus.Error;
                    result.ErrorMessage = $"项目路径不存在: {projectPath}";
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // 检查Linguist是否可用
                if (!await IsLinguistAvailableAsync())
                {
                    // 如果Linguist不可用，使用简单的文件扩展名分析
                    return await AnalyzeWithFileExtensions(projectPath, progress, cancellationToken);
                }

                // 使用Linguist进行分析
                return await AnalyzeWithLinguist(projectPath, progress, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                result.Status = LinguistAnalysisStatus.Cancelled;
                result.ErrorMessage = "分析已取消";
                result.EndTime = DateTime.Now;
                return result;
            }
            catch (Exception ex)
            {
                result.Status = LinguistAnalysisStatus.Error;
                result.ErrorMessage = $"分析失败: {ex.Message}";
                result.EndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// 使用Linguist进行分析
        /// </summary>
        private async Task<LinguistAnalysisResult> AnalyzeWithLinguist(
            string projectPath,
            IProgress<LinguistAnalysisResult>? progress,
            CancellationToken cancellationToken)
        {
            var result = new LinguistAnalysisResult
            {
                ProjectPath = projectPath,
                Status = LinguistAnalysisStatus.Initializing,
                StartTime = DateTime.Now
            };

            progress?.Report(result);

            try
            {
                // 执行linguist命令获取语言统计
                var linguistOutput = await RunLinguistCommand(projectPath, "--breakdown", cancellationToken);
                
                result.Status = LinguistAnalysisStatus.AnalyzingLanguages;
                result.Progress = 50;
                progress?.Report(result);

                // 解析linguist输出
                ParseLinguistOutput(linguistOutput, result);

                // 获取主要语言
                var primaryLanguage = await GetPrimaryLanguageAsync(projectPath);
                result.PrimaryLanguage = primaryLanguage;

                result.Status = LinguistAnalysisStatus.Completed;
                result.Progress = 100;
                result.EndTime = DateTime.Now;
                progress?.Report(result);

                return result;
            }
            catch (Exception ex)
            {
                result.Status = LinguistAnalysisStatus.Error;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// 使用文件扩展名进行简单分析（当Linguist不可用时）
        /// </summary>
        private async Task<LinguistAnalysisResult> AnalyzeWithFileExtensions(
            string projectPath,
            IProgress<LinguistAnalysisResult>? progress,
            CancellationToken cancellationToken)
        {
            var result = new LinguistAnalysisResult
            {
                ProjectPath = projectPath,
                Status = LinguistAnalysisStatus.ScanningFiles,
                StartTime = DateTime.Now
            };

            progress?.Report(result);

            var languageStats = new Dictionary<string, LanguageStatistics>();
            var extensionMap = GetFileExtensionLanguageMap();

            try
            {
                var files = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories)
                    .Where(f => !IsIgnoredPath(f))
                    .ToArray();

                result.TotalFiles = files.Length;
                result.Status = LinguistAnalysisStatus.AnalyzingLanguages;
                progress?.Report(result);

                for (int i = 0; i < files.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var file = files[i];
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    
                    if (extensionMap.TryGetValue(extension, out var language))
                    {
                        if (!languageStats.ContainsKey(language))
                        {
                            languageStats[language] = new LanguageStatistics
                            {
                                Language = language,
                                Color = _languageColors.GetValueOrDefault(language, "#cccccc")
                            };
                        }

                        var fileInfo = new FileInfo(file);
                        languageStats[language].FileCount++;
                        languageStats[language].ByteCount += fileInfo.Length;
                        
                        // 简单估算代码行数
                        try
                        {
                            var lines = await File.ReadAllLinesAsync(file, cancellationToken);
                            languageStats[language].LineCount += lines.Length;
                        }
                        catch
                        {
                            // 忽略读取错误的文件
                        }
                    }

                    result.ProcessedFiles = i + 1;
                    result.Progress = (int)((double)(i + 1) / files.Length * 90);
                    progress?.Report(result);
                }

                // 计算百分比
                var totalBytes = languageStats.Values.Sum(s => s.ByteCount);
                foreach (var stat in languageStats.Values)
                {
                    stat.Percentage = totalBytes > 0 ? (double)stat.ByteCount / totalBytes * 100 : 0;
                }

                // 设置主要语言
                var primaryLang = languageStats.Values.OrderByDescending(s => s.ByteCount).FirstOrDefault();
                if (primaryLang != null)
                {
                    primaryLang.IsPrimary = true;
                    result.PrimaryLanguage = primaryLang.Language;
                }

                result.Languages = languageStats;
                result.TotalLines = languageStats.Values.Sum(s => s.LineCount);
                result.TotalBytes = totalBytes;
                result.Status = LinguistAnalysisStatus.Completed;
                result.Progress = 100;
                result.EndTime = DateTime.Now;
                progress?.Report(result);

                return result;
            }
            catch (Exception ex)
            {
                result.Status = LinguistAnalysisStatus.Error;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// 检查Linguist是否可用
        /// </summary>
        public async Task<bool> IsLinguistAvailableAsync()
        {
            if (string.IsNullOrEmpty(_linguistPath))
                return false;

            try
            {
                var result = await RunCommandAsync(_linguistPath, "--version", null, CancellationToken.None);
                return !string.IsNullOrEmpty(result);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取Linguist版本信息
        /// </summary>
        public async Task<string?> GetLinguistVersionAsync()
        {
            if (!await IsLinguistAvailableAsync())
                return null;

            try
            {
                return await RunLinguistCommand(null, "--version", CancellationToken.None);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取支持的语言列表
        /// </summary>
        public async Task<List<string>> GetSupportedLanguagesAsync()
        {
            // 返回常见的编程语言列表
            return await Task.FromResult(new List<string>
            {
                "C#", "JavaScript", "TypeScript", "Python", "Java", "C++", "C",
                "Go", "Rust", "PHP", "Ruby", "Swift", "Kotlin", "Dart",
                "HTML", "CSS", "SCSS", "Less", "Vue", "React", "Angular",
                "JSON", "XML", "YAML", "TOML", "Markdown", "SQL"
            });
        }

        /// <summary>
        /// 分析单个文件的语言类型
        /// </summary>
        public async Task<string?> DetectFileLanguageAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var extensionMap = GetFileExtensionLanguageMap();
            
            return await Task.FromResult(extensionMap.GetValueOrDefault(extension));
        }

        /// <summary>
        /// 获取项目的主要语言
        /// </summary>
        public async Task<string?> GetPrimaryLanguageAsync(string projectPath)
        {
            if (!await IsLinguistAvailableAsync())
            {
                // 使用简单的文件扩展名分析
                var result = await AnalyzeWithFileExtensions(projectPath, null, CancellationToken.None);
                return result.PrimaryLanguage;
            }

            try
            {
                var output = await RunLinguistCommand(projectPath, "", CancellationToken.None);
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                // 解析linguist输出，第一行通常是主要语言
                foreach (var line in lines)
                {
                    var match = Regex.Match(line, @"^([\d.]+)%\s+(.+)$");
                    if (match.Success)
                    {
                        return match.Groups[2].Value.Trim();
                    }
                }
            }
            catch
            {
                // 忽略错误
            }

            return null;
        }

        /// <summary>
        /// 获取语言统计信息
        /// </summary>
        public async Task<Dictionary<string, LanguageStatistics>> GetLanguageStatisticsAsync(string projectPath)
        {
            var result = await AnalyzeProjectAsync(projectPath);
            return result.Languages;
        }

        /// <summary>
        /// 查找Linguist可执行文件
        /// </summary>
        private string? FindLinguistExecutable()
        {
            var possiblePaths = new[]
            {
                "linguist",
                "github-linguist",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gem", "bin", "linguist"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gem", "bin", "linguist.bat")
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = "--version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    process.WaitForExit(5000);
                    
                    if (process.ExitCode == 0)
                    {
                        return path;
                    }
                }
                catch
                {
                    // 继续尝试下一个路径
                }
            }

            return null;
        }

        /// <summary>
        /// 运行Linguist命令
        /// </summary>
        private async Task<string> RunLinguistCommand(string? workingDirectory, string arguments, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_linguistPath))
                throw new InvalidOperationException("Linguist不可用");

            return await RunCommandAsync(_linguistPath, arguments, workingDirectory, cancellationToken);
        }

        /// <summary>
        /// 运行命令行程序
        /// </summary>
        private async Task<string> RunCommandAsync(string fileName, string arguments, string? workingDirectory, CancellationToken cancellationToken)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
                }
            };

            process.Start();
            
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync(cancellationToken);
            
            var output = await outputTask;
            var error = await errorTask;
            
            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException($"命令执行失败: {error}");
            }
            
            return output;
        }

        /// <summary>
        /// 解析Linguist输出
        /// </summary>
        private void ParseLinguistOutput(string output, LinguistAnalysisResult result)
        {
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var languageStats = new Dictionary<string, LanguageStatistics>();
            
            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^([\d.]+)%\s+(.+)$");
                if (match.Success)
                {
                    var percentage = double.Parse(match.Groups[1].Value);
                    var language = match.Groups[2].Value.Trim();
                    
                    languageStats[language] = new LanguageStatistics
                    {
                        Language = language,
                        Percentage = percentage,
                        Color = _languageColors.GetValueOrDefault(language, "#cccccc")
                    };
                }
            }
            
            // 设置主要语言
            var primaryLang = languageStats.Values.OrderByDescending(s => s.Percentage).FirstOrDefault();
            if (primaryLang != null)
            {
                primaryLang.IsPrimary = true;
            }
            
            result.Languages = languageStats;
        }

        /// <summary>
        /// 获取文件扩展名到语言的映射
        /// </summary>
        private Dictionary<string, string> GetFileExtensionLanguageMap()
        {
            return new Dictionary<string, string>
            {
                {".cs", "C#"}, {".csx", "C#"}, {".cake", "C#"},
                {".js", "JavaScript"}, {".jsx", "JavaScript"}, {".mjs", "JavaScript"},
                {".ts", "TypeScript"}, {".tsx", "TypeScript"},
                {".py", "Python"}, {".pyw", "Python"}, {".py3", "Python"},
                {".java", "Java"}, {".class", "Java"}, {".jar", "Java"},
                {".cpp", "C++"}, {".cxx", "C++"}, {".cc", "C++"}, {".hpp", "C++"},
                {".c", "C"}, {".h", "C"},
                {".go", "Go"}, {".rs", "Rust"}, {".php", "PHP"}, {".rb", "Ruby"},
                {".swift", "Swift"}, {".kt", "Kotlin"}, {".dart", "Dart"},
                {".html", "HTML"}, {".htm", "HTML"}, {".xhtml", "HTML"},
                {".css", "CSS"}, {".scss", "SCSS"}, {".sass", "SCSS"}, {".less", "Less"},
                {".vue", "Vue"}, {".svelte", "Svelte"},
                {".json", "JSON"}, {".xml", "XML"}, {".yaml", "YAML"}, {".yml", "YAML"},
                {".toml", "TOML"}, {".md", "Markdown"}, {".markdown", "Markdown"},
                {".sql", "SQL"}, {".mysql", "SQL"}, {".pgsql", "SQL"},
                {".sh", "Shell"}, {".bash", "Shell"}, {".zsh", "Shell"}, {".fish", "Shell"},
                {".ps1", "PowerShell"}, {".psm1", "PowerShell"}, {".psd1", "PowerShell"},
                {".bat", "Batch"}, {".cmd", "Batch"}
            };
        }

        /// <summary>
        /// 初始化语言颜色映射
        /// </summary>
        private Dictionary<string, string> InitializeLanguageColors()
        {
            return new Dictionary<string, string>
            {
                {"C#", "#239120"}, {"JavaScript", "#f1e05a"}, {"TypeScript", "#2b7489"},
                {"Python", "#3572A5"}, {"Java", "#b07219"}, {"C++", "#f34b7d"},
                {"C", "#555555"}, {"Go", "#00ADD8"}, {"Rust", "#dea584"},
                {"PHP", "#4F5D95"}, {"Ruby", "#701516"}, {"Swift", "#ffac45"},
                {"Kotlin", "#F18E33"}, {"Dart", "#00B4AB"}, {"HTML", "#e34c26"},
                {"CSS", "#563d7c"}, {"SCSS", "#c6538c"}, {"Less", "#1d365d"},
                {"Vue", "#2c3e50"}, {"JSON", "#292929"}, {"XML", "#0060ac"},
                {"YAML", "#cb171e"}, {"Markdown", "#083fa1"}, {"SQL", "#336791"},
                {"Shell", "#89e051"}, {"PowerShell", "#012456"}, {"Batch", "#C1F12E"}
            };
        }

        /// <summary>
        /// 检查路径是否应该被忽略
        /// </summary>
        private bool IsIgnoredPath(string path)
        {
            var ignoredPatterns = new[]
            {
                ".git", ".svn", ".hg", ".bzr",
                "node_modules", "bower_components",
                "bin", "obj", "packages", ".vs", ".vscode",
                "target", "build", "dist", "out",
                ".idea", ".eclipse", ".metadata",
                "__pycache__", ".pytest_cache", ".tox",
                ".gradle", ".maven", ".m2"
            };

            return ignoredPatterns.Any(pattern => path.Contains(Path.DirectorySeparatorChar + pattern + Path.DirectorySeparatorChar) ||
                                                 path.EndsWith(Path.DirectorySeparatorChar + pattern));
        }
    }
}