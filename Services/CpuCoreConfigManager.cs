using System.Text;
using TSysWatch.Services.Models;

namespace TSysWatch.Services
{
    /// <summary>
    /// CPU 核心管理器配置服务
    /// </summary>
    public class CpuCoreConfigManager
    {
        private readonly string _configFilePath;
        private readonly ILogger<CpuCoreConfigManager> _logger;

        public CpuCoreConfigManager(ILogger<CpuCoreConfigManager> logger)
        {
            _logger = logger;
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CpuCoreManager.ini");
        }

        /// <summary>
        /// 读取配置文件
        /// </summary>
        public ProcessCoreConfig LoadConfig()
        {
            var config = new ProcessCoreConfig();

            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogWarning($"配置文件不存在: {_configFilePath}，将使用默认配置");
                    SaveConfig(config); // 创建默认配置文件
                    return config;
                }

                var lines = File.ReadAllLines(_configFilePath, Encoding.UTF8);
                string currentSection = "";

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        continue;
                    }

                    var parts = trimmedLine.Split('=', 2);
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    switch (currentSection.ToLower())
                    {
                        case "general":
                            ParseGeneralConfig(config, key, value);
                            break;
                        case "processname":
                            ParseProcessNameMapping(config, key, value);
                            break;
                        case "pid":
                            ParsePidMapping(config, key, value);
                            break;
                        case "processcorebinding":
                            ParseProcessCoreBindingMapping(config, key, value);
                            break; 
                        case "critical":
                            ParseCriticalProcesses(config, key, value);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"读取配置文件失败: {_configFilePath}");
            }

            return config;
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        public bool SaveConfig(ProcessCoreConfig config)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# CPU 核心数管理器配置文件");
                sb.AppendLine("# 支持的配置节：[General], [ProcessName], [PID], [ProcessCoreBinding], [Critical]");
                sb.AppendLine();

                // General 配置
                sb.AppendLine("[General]");
                sb.AppendLine($"DefaultCoreCount={config.DefaultCoreCount}");
                sb.AppendLine($"ScanIntervalSeconds={config.ScanIntervalSeconds}");
                sb.AppendLine($"Enabled={config.Enabled}");
                sb.AppendLine();

                // 进程名核心数映射
                sb.AppendLine("[ProcessName]");
                sb.AppendLine("# 格式: 进程名=核心数");
                foreach (var kvp in config.ProcessNameMapping)
                {
                    sb.AppendLine($"{kvp.Key}={kvp.Value}");
                }
                sb.AppendLine();

                // PID 核心数映射
                sb.AppendLine("[PID]");
                sb.AppendLine("# 格式: PID=核心数 (优先级高于进程名)");
                foreach (var kvp in config.PidMapping)
                {
                    sb.AppendLine($"{kvp.Key}={kvp.Value}");
                }
                sb.AppendLine();

                // 进程名核心绑定映射
                sb.AppendLine("[ProcessCoreBinding]");
                sb.AppendLine("# 格式: 进程名=核心索引列表(用逗号分隔) 例: chrome=0,2,4");
                sb.AppendLine("# 核心绑定优先级高于核心数设置");
                foreach (var kvp in config.ProcessCoreBindingMapping)
                {
                    sb.AppendLine($"{kvp.Key}={kvp.Value}");
                }
                sb.AppendLine();

                
                // 关键进程
                sb.AppendLine("[Critical]");
                sb.AppendLine("# 系统关键进程列表，用逗号分隔");
                sb.AppendLine($"Processes={string.Join(",", config.CriticalProcesses)}");

                File.WriteAllText(_configFilePath, sb.ToString(), Encoding.UTF8);
                _logger.LogInformation($"配置文件保存成功: {_configFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"保存配置文件失败: {_configFilePath}");
                return false;
            }
        }

        private void ParseGeneralConfig(ProcessCoreConfig config, string key, string value)
        {
            switch (key.ToLower())
            {
                case "defaultcorecount":
                    if (int.TryParse(value, out int defaultCore))
                        config.DefaultCoreCount = Math.Min(defaultCore, Environment.ProcessorCount);
                    break;
                case "scanintervalseconds":
                    if (int.TryParse(value, out int interval))
                        config.ScanIntervalSeconds = Math.Max(1, interval);
                    break;
                case "enabled":
                    if (bool.TryParse(value, out bool enabled))
                        config.Enabled = enabled;
                    break;
            }
        }

        private void ParseProcessNameMapping(ProcessCoreConfig config, string key, string value)
        {
            if (int.TryParse(value, out int coreCount))
            {
                config.ProcessNameMapping[key] = Math.Min(coreCount, Environment.ProcessorCount);
            }
        }

        private void ParsePidMapping(ProcessCoreConfig config, string key, string value)
        {
            if (int.TryParse(key, out int pid) && int.TryParse(value, out int coreCount))
            {
                config.PidMapping[pid] = Math.Min(coreCount, Environment.ProcessorCount);
            }
        }

        private void ParseProcessCoreBindingMapping(ProcessCoreConfig config, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                // 验证核心绑定格式
                var cores = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => int.TryParse(s, out int core) && core >= 0 && core < Environment.ProcessorCount);
                
                if (cores.Any())
                {
                    config.ProcessCoreBindingMapping[key] = string.Join(",", cores);
                }
            }
        }
         

        private void ParseCriticalProcesses(ProcessCoreConfig config, string key, string value)
        {
            if (key.ToLower() == "processes")
            {
                var processes = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p));
                
                config.CriticalProcesses.Clear();
                foreach (var process in processes)
                {
                    config.CriticalProcesses.Add(process);
                }
            }
        }

        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        public string GetConfigFilePath() => _configFilePath;
    }
}