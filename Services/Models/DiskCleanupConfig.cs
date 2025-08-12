namespace TSysWatch
{
    /// <summary>
    /// 磁盘清理配置
    /// </summary>
    public class DiskCleanupConfig
    {
        /// <summary>
        /// 磁盘驱动器（如：C:）
        /// </summary>
        public string DriveLetter { get; set; }
        
        /// <summary>
        /// 删除目录列表
        /// </summary>
        public List<string> DeleteDirectories { get; set; } = new List<string>();
        
        /// <summary>
        /// 开始删除磁盘大小（GB）
        /// </summary>
        public double StartDeleteSizeGB { get; set; }
        
        /// <summary>
        /// 停止删除磁盘大小（GB）
        /// </summary>
        public double StopDeleteSizeGB { get; set; }
    }
}
