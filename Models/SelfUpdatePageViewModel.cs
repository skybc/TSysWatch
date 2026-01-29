namespace TSysWatch.Models
{
    /// <summary>
    /// 自更新页面的 ViewModel
    /// </summary>
    public class SelfUpdatePageViewModel
    {
        /// <summary>
        /// 当前应用版本
        /// </summary>
        public string CurrentVersion { get; set; } = "1.0.0";

        /// <summary>
        /// 构建时间
        /// </summary>
        public DateTime? BuildTime { get; set; }

        /// <summary>
        /// 应用类型（aspnetcore / framework 等）
        /// </summary>
        public string AppType { get; set; } = "aspnetcore";

        /// <summary>
        /// 当前运行路径
        /// </summary>
        public string RunningPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 最大上传文件大小（MB）
        /// </summary>
        public int MaxUploadSizeMB { get; set; } = 500;
    }
}
