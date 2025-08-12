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
        public string DriveLetter { get; set; } = string.Empty;
        
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
        
        /// <summary>
        /// 开始删除文件时间（天）- 只删除超过N天的文件
        /// </summary>
        public int StartDeleteFileDays { get; set; } = 0;
        
        /// <summary>
        /// 删除条件逻辑关系：AND（且）或 OR（或）
        /// AND: 同时满足容量和时间条件才删除
        /// OR: 满足容量或时间条件之一即可删除
        /// </summary>
        public DeleteLogicMode LogicMode { get; set; } = DeleteLogicMode.OR;
    }

    /// <summary>
    /// 删除条件逻辑模式
    /// </summary>
    public enum DeleteLogicMode
    {
        /// <summary>
        /// 或 - 满足容量或时间条件之一即可删除
        /// </summary>
        OR,
        
        /// <summary>
        /// 且 - 必须同时满足容量和时间条件才删除
        /// </summary>
        AND
    }
}
