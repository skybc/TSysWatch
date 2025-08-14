namespace TSysWatch.Entity
{
    /// <summary>
    /// 文件管理视图模型
    /// </summary>
    public class FileViewModel
    {
        /// <summary>
        /// 文件或文件夹名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 文件或文件夹完整路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 是否是文件夹
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 格式化的文件大小
        /// </summary>
        public string FormattedSize
        {
            get
            {
                if (IsDirectory) return "--";
                
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = Size;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }

        /// <summary>
        /// 是否在系统盘
        /// </summary>
        public bool IsInSystemDrive
        {
            get
            {
                var drive = System.IO.Path.GetPathRoot(Path)?.ToUpper();
                return drive == "C:\\" || string.IsNullOrEmpty(drive);
            }
        }
    }

    /// <summary>
    /// 文件管理页面视图模型
    /// </summary>
    public class FileManagerViewModel
    {
        /// <summary>
        /// 当前路径
        /// </summary>
        public string CurrentPath { get; set; } = string.Empty;

        /// <summary>
        /// 文件和文件夹列表
        /// </summary>
        public List<FileViewModel> Files { get; set; } = new List<FileViewModel>();

        /// <summary>
        /// 面包屑导航
        /// </summary>
        public List<BreadcrumbItem> Breadcrumbs { get; set; } = new List<BreadcrumbItem>();

        /// <summary>
        /// 可用驱动器列表（不包含C盘）
        /// </summary>
        public List<DriveViewModel> AvailableDrives { get; set; } = new List<DriveViewModel>();

        /// <summary>
        /// 是否可以删除（非系统盘）
        /// </summary>
        public bool CanDelete
        {
            get
            {
                var drive = System.IO.Path.GetPathRoot(CurrentPath)?.ToUpper();
                return drive != "C:\\";
            }
        }
    }

    /// <summary>
    /// 面包屑导航项
    /// </summary>
    public class BreadcrumbItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    /// <summary>
    /// 驱动器视图模型
    /// </summary>
    public class DriveViewModel
    {
        /// <summary>
        /// 驱动器名称（如 D:\）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 驱动器标签（如果有）
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 驱动器类型
        /// </summary>
        public string DriveType { get; set; } = string.Empty;

        /// <summary>
        /// 总空间（字节）
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// 可用空间（字节）
        /// </summary>
        public long AvailableSpace { get; set; }

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// 格式化的总空间
        /// </summary>
        public string FormattedTotalSize
        {
            get
            {
                if (!IsReady) return "--";
                return FormatBytes(TotalSize);
            }
        }

        /// <summary>
        /// 格式化的可用空间
        /// </summary>
        public string FormattedAvailableSpace
        {
            get
            {
                if (!IsReady) return "--";
                return FormatBytes(AvailableSpace);
            }
        }

        /// <summary>
        /// 使用百分比
        /// </summary>
        public double UsagePercentage
        {
            get
            {
                if (!IsReady || TotalSize == 0) return 0;
                return (double)(TotalSize - AvailableSpace) / TotalSize * 100;
            }
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(Label))
                    return $"{Name} ({DriveType})";
                return $"{Label} ({Name}) - {DriveType}";
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
