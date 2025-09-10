using TSysWatch.Services.Models;

namespace TSysWatch.Models
{
    /// <summary>
    /// CPU 核心管理器视图模型
    /// </summary>
    public class CpuCoreIndexViewModel
    {
        /// <summary>
        /// 配置信息
        /// </summary>
        public ProcessCoreConfig Config { get; set; } = new();

        /// <summary>
        /// 进程列表
        /// </summary>
        public List<ProcessInfo> Processes { get; set; } = new();

        /// <summary>
        /// 最近日志
        /// </summary>
        public List<ProcessAffinityLog> RecentLogs { get; set; } = new();

        /// <summary>
        /// 系统信息
        /// </summary>
        public SystemInfo SystemInfo { get; set; } = new();
    }

    /// <summary>
    /// 系统信息模型
    /// </summary>
    public class SystemInfo
    {
        /// <summary>
        /// 处理器核心数
        /// </summary>
        public int ProcessorCount { get; set; }

        /// <summary>
        /// 是否具有管理员权限
        /// </summary>
        public bool IsAdministrator { get; set; }

        /// <summary>
        /// 配置文件路径
        /// </summary>
        public string ConfigFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 机器名称
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>
        /// 操作系统版本
        /// </summary>
        public string OSVersion { get; set; } = string.Empty;

        /// <summary>
        /// 工作集大小
        /// </summary>
        public long WorkingSet { get; set; }

        /// <summary>
        /// 系统运行时间
        /// </summary>
        public long TickCount { get; set; }
    }
}