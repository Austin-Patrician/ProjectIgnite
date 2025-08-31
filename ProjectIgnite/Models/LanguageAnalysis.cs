using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectIgnite.Models
{
    /// <summary>
    /// 语言分析结果实体类，对应LANGUAGE_ANALYSIS表
    /// </summary>
    public class LanguageAnalysis
    {
        /// <summary>
        /// 分析记录ID，主键
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 关联的项目ID，外键
        /// </summary>
        [Required]
        public int ProjectId { get; set; }

        /// <summary>
        /// 编程语言名称
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// 该语言的代码行数
        /// </summary>
        public long LineCount { get; set; }

        /// <summary>
        /// 该语言的文件数量
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// 该语言占项目的百分比
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal Percentage { get; set; }

        /// <summary>
        /// 该语言的字节数
        /// </summary>
        public long ByteCount { get; set; }

        /// <summary>
        /// 分析时间
        /// </summary>
        public DateTime AnalyzedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 导航属性：关联的项目源
        /// </summary>
        [ForeignKey("ProjectId")]
        public virtual ProjectSource? ProjectSource { get; set; }
    }
}