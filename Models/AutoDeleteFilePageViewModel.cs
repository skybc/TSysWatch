using System.Collections.Generic;

namespace TSysWatch
{
    /// <summary>
    /// 自动删除文件页面 ViewModel
    /// </summary>
    public class AutoDeleteFilePageViewModel
    {
        /// <summary>
        /// 磁盘驱动器信息列表
        /// </summary>
        public List<DriveInfoDisplay> Drives { get; set; } = new();

        /// <summary>
        /// 删除配置列表
        /// </summary>
        public List<DiskCleanupConfigDisplay> Configs { get; set; } = new();
    }

    /// <summary>
    /// 驱动器信息显示对象
    /// </summary>
    public class DriveInfoDisplay
    {
        /// <summary>
        /// 驱动器字母（如 C:）
        /// </summary>
        public string DriveLetter { get; set; } = "";

        /// <summary>
        /// 总容量
        /// </summary>
        public string TotalSize { get; set; } = "";

        /// <summary>
        /// 剩余空间
        /// </summary>
        public string FreeSpace { get; set; } = "";

        /// <summary>
        /// 剩余空间（GB）
        /// </summary>
        public double FreeSpaceGB { get; set; }
    }

    /// <summary>
    /// 磁盘清理配置显示对象
    /// </summary>
    public class DiskCleanupConfigDisplay
    {
        /// <summary>
        /// 驱动器字母
        /// </summary>
        public string DriveLetter { get; set; } = "";

        /// <summary>
        /// 删除目录列表
        /// </summary>
        public List<string> DeleteDirectories { get; set; } = new();

        /// <summary>
        /// 开始删除大小（GB）
        /// </summary>
        public double StartDeleteSizeGB { get; set; }

        /// <summary>
        /// 停止删除大小（GB）
        /// </summary>
        public double StopDeleteSizeGB { get; set; }

        /// <summary>
        /// 开始删除文件天数
        /// </summary>
        public int StartDeleteFileDays { get; set; }

        /// <summary>
        /// 逻辑模式（OR/AND）
        /// </summary>
        public string LogicMode { get; set; } = "";
    }
}
