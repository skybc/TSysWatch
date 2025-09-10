using System.ComponentModel.DataAnnotations;
using TSysWatch.Services.Models;

namespace TSysWatch.Services.Models
{
    /// <summary>
    /// 进程核心数配置模型
    /// </summary>
    public class ProcessCoreConfig
    {
        /// <summary>
        /// 默认核心数
        /// </summary>
        public int DefaultCoreCount { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// 扫描间隔（秒）
        /// </summary>
        public int ScanIntervalSeconds { get; set; } = 2;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 进程名称到核心数的映射
        /// </summary>
        public Dictionary<string, int> ProcessNameMapping { get; set; } = new();

        /// <summary>
        /// PID到核心数的映射（优先级高于进程名）
        /// </summary>
        public Dictionary<int, int> PidMapping { get; set; } = new();

        /// <summary>
        /// 进程名称到具体核心绑定的映射（优先级高于核心数设置）
        /// Key: 进程名, Value: 核心索引列表（如 "0,2,4" 表示绑定到核心 0、2、4）
        /// </summary>
        public Dictionary<string, string> ProcessCoreBindingMapping { get; set; } = new();
 

        /// <summary>
        /// 系统关键进程列表（跳过设置）
        /// </summary>
        public HashSet<string> CriticalProcesses { get; set; } = new()
        {
            "System", "csrss", "wininit", "winlogon", "services", "lsass", "dwm", "explorer"
        };
    }

    /// <summary>
    /// 进程信息
    /// </summary>
    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public int ConfiguredCoreCount { get; set; }
        public int CurrentCoreCount { get; set; }
        
        /// <summary>
        /// 当前亲和性掩码（十六进制字符串格式）
        /// </summary>
        public string CurrentAffinityMask { get; set; } = "0x0";
        
        /// <summary>
        /// 配置的核心绑定（如果设置了具体核心绑定）
        /// </summary>
        public string? ConfiguredCoreBinding { get; set; }
        
        /// <summary>
        /// 当前绑定的核心列表
        /// </summary>
        public List<int> CurrentBoundCores { get; set; } = new();
        
        public DateTime LastUpdated { get; set; }
        public bool IsSystemCritical { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 操作日志
    /// </summary>
    public class ProcessAffinityLog
    {
        public DateTime Timestamp { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        
        /// <summary>
        /// 旧亲和性掩码（十六进制字符串格式）
        /// </summary>
        public string OldAffinityMask { get; set; } = "0x0";
        
        /// <summary>
        /// 新亲和性掩码（十六进制字符串格式）
        /// </summary>
        public string NewAffinityMask { get; set; } = "0x0";
        
        /// <summary>
        /// 设置类型（CoreCount 或 CoreBinding）
        /// </summary>
        public string SettingType { get; set; } = "CoreCount";
        
        /// <summary>
        /// 核心绑定详情（如果是核心绑定设置）
        /// </summary>
        public string? CoreBindingDetails { get; set; }
        
        public bool Success { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 核心绑定设置请求
    /// </summary>
    public class CoreBindingRequest
    {
        /// <summary>
        /// 进程ID
        /// </summary>
        public int ProcessId { get; set; }
        
        /// <summary>
        /// 要绑定的核心索引列表
        /// </summary>
        public List<int> CoreIndices { get; set; } = new();
    }

    /// <summary>
    /// 进程核心绑定映射请求
    /// </summary>
    public class ProcessCoreBindingMappingRequest
    {
        /// <summary>
        /// 进程名称
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;
        
        /// <summary>
        /// 要绑定的核心索引列表
        /// </summary>
        public List<int> CoreIndices { get; set; } = new();
    }
}