using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using TSysWatch.Services.Models;

namespace TSysWatch.Services
{
    /// <summary>
    /// CPU 核心数管理器
    /// </summary>
    public class CpuCoreManager
    {
        private readonly ILogger<CpuCoreManager> _logger;
        private readonly ProcessCoreConfig _config;
        private readonly List<ProcessAffinityLog> _logs = new();
        private readonly object _lockObject = new object();

        public CpuCoreManager(ILogger<CpuCoreManager> logger)
        {
            _logger = logger;
            _config = new ProcessCoreConfig();
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public ProcessCoreConfig GetConfig() => _config;

        /// <summary>
        /// 更新配置
        /// </summary>
        public void UpdateConfig(ProcessCoreConfig newConfig)
        {
            lock (_lockObject)
            {
                _config.DefaultCoreCount = Math.Min(newConfig.DefaultCoreCount, Environment.ProcessorCount);
                _config.ScanIntervalSeconds = Math.Max(1, newConfig.ScanIntervalSeconds);
                _config.Enabled = newConfig.Enabled;
                
                _config.ProcessNameMapping.Clear();
                foreach (var kvp in newConfig.ProcessNameMapping)
                {
                    _config.ProcessNameMapping[kvp.Key] = Math.Min(kvp.Value, Environment.ProcessorCount);
                }
                
                _config.PidMapping.Clear();
                foreach (var kvp in newConfig.PidMapping)
                {
                    _config.PidMapping[kvp.Key] = Math.Min(kvp.Value, Environment.ProcessorCount);
                }

                // 新增：核心绑定映射
                _config.ProcessCoreBindingMapping.Clear();
                foreach (var kvp in newConfig.ProcessCoreBindingMapping)
                {
                    if (ValidateCoreBinding(kvp.Value))
                    {
                        _config.ProcessCoreBindingMapping[kvp.Key] = kvp.Value;
                    }
                }

               

                // 更新关键进程列表
                _config.CriticalProcesses.Clear();
                foreach (var process in newConfig.CriticalProcesses)
                {
                    _config.CriticalProcesses.Add(process);
                }
            }
        }

        /// <summary>
        /// 为进程设置 CPU 核心数
        /// </summary>
        public bool SetProcessCoreCount(int processId, int coreCount)
        {
            if (coreCount <= 0 || coreCount > Environment.ProcessorCount)
            {
                coreCount = Math.Min(Math.Max(1, coreCount), Environment.ProcessorCount);
            }

            try
            {
                IntPtr processHandle = WindowsApi.OpenProcess(
                    WindowsApi.PROCESS_SET_INFORMATION | WindowsApi.PROCESS_QUERY_INFORMATION,
                    false, processId);

                if (processHandle == IntPtr.Zero)
                {
                    LogOperation(processId, "Unknown", IntPtr.Zero, IntPtr.Zero, false, 
                        $"无法打开进程句柄，错误代码: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                try
                {
                    // 获取当前亲和性掩码
                    IntPtr currentMask, systemMask;
                    if (!WindowsApi.GetProcessAffinityMask(processHandle, out currentMask, out systemMask))
                    {
                        LogOperation(processId, "Unknown", IntPtr.Zero, IntPtr.Zero, false, 
                            "无法获取当前亲和性掩码");
                        return false;
                    }

                    // 计算新的亲和性掩码
                    IntPtr newMask = CalculateAffinityMask(coreCount);

                    // 如果掩码相同，跳过设置
                    if (currentMask == newMask)
                    {
                        return true;
                    }

                    // 设置新的亲和性掩码
                    bool success = WindowsApi.SetProcessAffinityMask(processHandle, newMask);
                    
                    string processName = GetProcessName(processId);
                    LogOperation(processId, processName, currentMask, newMask, success,
                        success ? "设置成功" : $"设置失败，错误代码: {Marshal.GetLastWin32Error()}", "CoreCount");


                    return success;
                }
                finally
                {
                    WindowsApi.CloseHandle(processHandle);
                }
            }
            catch (Exception ex)
            {
                LogOperation(processId, "Unknown", IntPtr.Zero, IntPtr.Zero, false, 
                    $"异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 为进程设置具体的CPU核心绑定
        /// </summary>
        public bool SetProcessCoreBinding(int processId, List<int> coreIndices)
        {
            if (coreIndices == null || coreIndices.Count == 0)
            {
                return false;
            }

            // 验证核心索引
            var validCores = coreIndices.Where(c => c >= 0 && c < Environment.ProcessorCount).ToList();
            if (validCores.Count == 0)
            {
                LogOperation(processId, "Unknown", IntPtr.Zero, IntPtr.Zero, false, 
                    $"无效的核心索引: {string.Join(",", coreIndices)}", "CoreBinding", string.Join(",", coreIndices));
                return false;
            }

            try
            {
                IntPtr processHandle = WindowsApi.OpenProcess(
                    WindowsApi.PROCESS_SET_INFORMATION | WindowsApi.PROCESS_QUERY_INFORMATION,
                    false, processId);

                if (processHandle == IntPtr.Zero)
                {
                    LogOperation(processId, "Unknown", IntPtr.Zero, IntPtr.Zero, false, 
                        $"无法打开进程句柄，错误代码: {Marshal.GetLastWin32Error()}", "CoreBinding", string.Join(",", validCores));
                    return false;
                }

                try
                {
                    // 获取当前亲和性掩码
                    IntPtr currentMask, systemMask;
                    if (!WindowsApi.GetProcessAffinityMask(processHandle, out currentMask, out systemMask))
                    {
                        LogOperation(processId, "Unknown", IntPtr.Zero, IntPtr.Zero, false, 
                            "无法获取当前亲和性掩码", "CoreBinding", string.Join(",", validCores));
                        return false;
                    }

                    // 计算新的亲和性掩码（基于具体核心索引）
                    IntPtr newMask = CalculateAffinityMaskFromCores(validCores);

                    // 如果掩码相同，跳过设置
                    if (currentMask == newMask)
                    {
                        return true;
                    }

                    // 设置新的亲和性掩码
                    bool success = WindowsApi.SetProcessAffinityMask(processHandle, newMask);
                    
                    string processName = GetProcessName(processId);
                    LogOperation(processId, processName, currentMask, newMask, success,
                        success ? "核心绑定设置成功" : $"核心绑定设置失败，错误代码: {Marshal.GetLastWin32Error()}", 
                        "CoreBinding", string.Join(",", validCores));

                    return success;
                }
                finally
                {
                    WindowsApi.CloseHandle(processHandle);
                }
            }
            catch (Exception ex)
            {
                LogOperation(processId, "Unknown", IntPtr.Zero, IntPtr.Zero, false, 
                    $"异常: {ex.Message}", "CoreBinding", string.Join(",", validCores));
                return false;
            }
        }

        /// <summary>
        /// 扫描所有进程并应用配置
        /// </summary>
        public void ScanAndApplyConfigurations()
        {
            if (!_config.Enabled)
                return;

            try
            {
                var processes = Process.GetProcesses();
                foreach (var process in processes)
                {
                    try
                    {
                        // 跳过系统关键进程
                        if (_config.CriticalProcesses.Contains(process.ProcessName))
                            continue;

                        var (targetCoreCount, coreBinding) = GetTargetCoreConfiguration(process.Id, process.ProcessName);
                        
                        if (coreBinding != null && coreBinding.Count > 0)
                        {
                            // 使用具体的核心绑定
                            SetProcessCoreBinding(process.Id, coreBinding);
                        }
                        else if (targetCoreCount > 0)
                        {
                            // 使用核心数量设置
                            SetProcessCoreCount(process.Id, targetCoreCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"处理进程 {process.Id} 时出错: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描进程时出错");
            }
        }

        /// <summary>
        /// 获取目标核心数或核心绑定
        /// </summary>
        private (int coreCount, List<int>? coreBinding) GetTargetCoreConfiguration(int processId, string processName)
        {
            lock (_lockObject)
            {
                
                // 2. 进程名核心绑定映射
                if (_config.ProcessCoreBindingMapping.TryGetValue(processName, out string? nameBinding))
                {
                    var cores = ParseCoreBinding(nameBinding);
                    if (cores.Count > 0)
                    {
                        return (cores.Count, cores);
                    }
                }

                // 3. PID 核心数映射
                if (_config.PidMapping.TryGetValue(processId, out int pidCoreCount))
                    return (pidCoreCount, null);

                // 4. 进程名核心数映射
                if (_config.ProcessNameMapping.TryGetValue(processName, out int nameCoreCount))
                    return (nameCoreCount, null);

                // 5. 默认核心数
                return (_config.DefaultCoreCount, null);
            }
        }
         

        /// <summary>
        /// 计算亲和性掩码
        /// </summary>
        private IntPtr CalculateAffinityMask(int coreCount)
        {
            if (coreCount <= 0) return new IntPtr(1);
            if (coreCount >= Environment.ProcessorCount)
                return new IntPtr((1L << Environment.ProcessorCount) - 1);

            return new IntPtr((1L << coreCount) - 1);
        }

        /// <summary>
        /// 根据核心索引列表计算亲和性掩码
        /// </summary>
        private IntPtr CalculateAffinityMaskFromCores(List<int> coreIndices)
        {
            long mask = 0;
            foreach (int coreIndex in coreIndices)
            {
                if (coreIndex >= 0 && coreIndex < Environment.ProcessorCount)
                {
                    mask |= (1L << coreIndex);
                }
            }
            return new IntPtr(mask == 0 ? 1 : mask);
        }

        /// <summary>
        /// 获取进程名称
        /// </summary>
        private string GetProcessName(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return process.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 记录操作日志
        /// </summary>
        private void LogOperation(int processId, string processName, IntPtr oldMask, 
            IntPtr newMask, bool success, string reason, string settingType = "CoreCount", string? coreBindingDetails = null)
        {
            lock (_lockObject)
            {
                var log = new ProcessAffinityLog
                {
                    Timestamp = DateTime.Now,
                    ProcessId = processId,
                    ProcessName = processName,
                    OldAffinityMask = $"0x{oldMask.ToInt64():X}",
                    NewAffinityMask = $"0x{newMask.ToInt64():X}",
                    SettingType = settingType,
                    CoreBindingDetails = coreBindingDetails,
                    Success = success,
                    Reason = reason
                };

                _logs.Add(log);

                // 保持日志数量在合理范围内
                if (_logs.Count > 1000)
                {
                    _logs.RemoveRange(0, 100);
                }

                string bindingInfo = !string.IsNullOrEmpty(coreBindingDetails) ? $", 核心绑定:{coreBindingDetails}" : "";
                _logger.LogInformation($"进程亲和性设置 - PID:{processId}, 名称:{processName}, " +
                    $"旧掩码:0x{oldMask:X}, 新掩码:0x{newMask:X}, 类型:{settingType}{bindingInfo}, 成功:{success}, 原因:{reason}");
            }
        }

        /// <summary>
        /// 获取最近的操作日志
        /// </summary>
        public List<ProcessAffinityLog> GetRecentLogs(int count = 100)
        {
            lock (_lockObject)
            {
                return _logs.TakeLast(count).ToList();
            }
        }

        /// <summary>
        /// 获取当前所有进程信息
        /// </summary>
        public List<ProcessInfo> GetCurrentProcesses()
        {
            var result = new List<ProcessInfo>();
            
            try
            {
                var processes = Process.GetProcesses();
                foreach (var process in processes)
                {
                    try
                    {
                        var (configuredCoreCount, coreBinding) = GetTargetCoreConfiguration(process.Id, process.ProcessName);
                        
                        var info = new ProcessInfo
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            ConfiguredCoreCount = configuredCoreCount,
                            ConfiguredCoreBinding = coreBinding != null ? string.Join(",", coreBinding) : null,
                            IsSystemCritical = _config.CriticalProcesses.Contains(process.ProcessName),
                            LastUpdated = DateTime.Now,
                            Status = "Running"
                        };

                        // 尝试获取当前亲和性掩码
                        try
                        {
                            IntPtr processHandle = WindowsApi.OpenProcess(
                                WindowsApi.PROCESS_QUERY_INFORMATION, false, process.Id);
                            
                            if (processHandle != IntPtr.Zero)
                            {
                                if (WindowsApi.GetProcessAffinityMask(processHandle, out IntPtr currentMask, out _))
                                {
                                    info.CurrentAffinityMask = $"0x{currentMask.ToInt64():X}";
                                    info.CurrentCoreCount = CountBitsInMask(currentMask);
                                    info.CurrentBoundCores = GetBoundCoresFromMask(currentMask);
                                }
                                WindowsApi.CloseHandle(processHandle);
                            }
                            else
                            {
                                info.CurrentAffinityMask = "0x0";
                                info.CurrentCoreCount = 0;
                                info.CurrentBoundCores = new List<int>();
                            }
                        }
                        catch
                        {
                            info.Status = "Access Denied";
                            info.CurrentAffinityMask = "0x0";
                            info.CurrentCoreCount = 0;
                            info.CurrentBoundCores = new List<int>();
                        }

                        result.Add(info);
                    }
                    catch
                    {
                        // 忽略无法访问的进程
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取进程列表时出错");
            }

            return result;
        }

        /// <summary>
        /// 计算掩码中的位数
        /// </summary>
        private int CountBitsInMask(IntPtr mask)
        {
            long value = mask.ToInt64();
            int count = 0;
            while (value > 0)
            {
                count += (int)(value & 1);
                value >>= 1;
            }
            return count;
        }

        /// <summary>
        /// 从亲和性掩码获取绑定的核心列表
        /// </summary>
        private List<int> GetBoundCoresFromMask(IntPtr mask)
        {
            var cores = new List<int>();
            long value = mask.ToInt64();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                if ((value & (1L << i)) != 0)
                {
                    cores.Add(i);
                }
            }
            return cores;
        }

        /// <summary>
        /// 验证核心绑定
        /// </summary>
        private bool ValidateCoreBinding(List<int> coreBinding)
        {
            if (coreBinding == null || coreBinding.Count == 0)
                return false;

            foreach (var core in coreBinding)
            {
                if (core < 0 || core >= Environment.ProcessorCount)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 验证核心绑定字符串格式
        /// </summary>
        private bool ValidateCoreBinding(string coreBinding)
        {
            if (string.IsNullOrWhiteSpace(coreBinding))
                return false;

            var parts = coreBinding.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (!int.TryParse(part.Trim(), out int coreIndex) || 
                    coreIndex < 0 || coreIndex >= Environment.ProcessorCount)
                {
                    return false;
                }
            }
            return parts.Length > 0;
        }

        /// <summary>
        /// 将核心绑定字符串转换为核心索引列表
        /// </summary>
        private List<int> ParseCoreBinding(string coreBinding)
        {
            if (string.IsNullOrWhiteSpace(coreBinding))
                return new List<int>();

            return coreBinding.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out int core) ? core : -1)
                .Where(core => core >= 0 && core < Environment.ProcessorCount)
                .ToList();
        }
    }
}