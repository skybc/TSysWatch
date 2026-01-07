using System.Collections.Generic;

namespace TSysWatch
{
    /// <summary>
    /// 自动拷贝文件页面 ViewModel
    /// </summary>
    public class AutoCopyFilePageViewModel
    {
        /// <summary>
        /// 可用磁盘列表
        /// </summary>
        public List<DriveOption> AvailableDrives { get; set; } = new();

        /// <summary>
        /// 拷贝配置列表
        /// </summary>
        public List<AutoCopyConfigDisplay> Configs { get; set; } = new();

        /// <summary>
        /// 磁盘选项
        /// </summary>
        public class DriveOption
        {
            /// <summary>
            /// 磁盘名称（如 E:）
            /// </summary>
            public string Name { get; set; } = "";

            /// <summary>
            /// 磁盘标签（如 Data Drive）
            /// </summary>
            public string Label { get; set; } = "";
        }
    }

    /// <summary>
    /// 拷贝配置显示对象
    /// </summary>
    public class AutoCopyConfigDisplay
    {
        /// <summary>
        /// 源目录
        /// </summary>
        public string SourceDirectory { get; set; } = "";

        /// <summary>
        /// 目标磁盘
        /// </summary>
        public string TargetDrive { get; set; } = "";

        /// <summary>
        /// 已拷贝文件目录
        /// </summary>
        public string? MovedDirectory { get; set; }

        /// <summary>
        /// 源目录是否存在
        /// </summary>
        public bool SourceDirExists { get; set; }

        /// <summary>
        /// 目标磁盘是否存在
        /// </summary>
        public bool TargetDriveExists { get; set; }

        /// <summary>
        /// 图片文件数
        /// </summary>
        public int ImageFileCount { get; set; }

        /// <summary>
        /// 目录大小（格式化字符串）
        /// </summary>
        public string DirectorySize { get; set; } = "";
    }
}
