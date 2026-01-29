namespace TSysWatch.Models
{
    /// <summary>
    /// 硬件监控配置
    /// </summary>
    public class HardwareMonitorConfig
    {
        /// <summary>
        /// 是否启用定时记录
        /// </summary>
        public bool EnableTimedRecording { get; set; } = false;

        /// <summary>
        /// 定时记录间隔（秒），最小为2秒
        /// </summary>
        public int RecordingIntervalSeconds { get; set; } = 10;

        /// <summary>
        /// CSV文件存储目录
        /// </summary>
        public string CsvStoragePath { get; set; } = "HardwareData";

        /// <summary>
        /// 需要记录的硬件传感器类型列表
        /// </summary>
        public List<string> RecordedSensorTypes { get; set; } = new();

        /// <summary>
        /// 获取完整的CSV存储路径
        /// </summary>
        public string GetFullCsvPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CsvStoragePath);
        }

        /// <summary>
        /// 验证配置有效性
        /// </summary>
        public bool ValidateConfig()
        {
            if (RecordingIntervalSeconds < 2)
            {
                RecordingIntervalSeconds = 2;
            }
            return true;
        }
    }
}
