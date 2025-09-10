using Microsoft.AspNetCore.Mvc;
using TSysWatch.Services;
using TSysWatch.Services.Models;
using TSysWatch.Models;

namespace TSysWatch.Controllers
{
    /// <summary>
    /// CPU 核心管理器控制器
    /// </summary>
    public class CpuCoreController : Controller
    {
        private readonly ILogger<CpuCoreController> _logger;
        private readonly CpuCoreManager _coreManager;
        private readonly CpuCoreConfigManager _configManager;
        private readonly ICpuCoreManagerService _managerService;

        public CpuCoreController(
            ILogger<CpuCoreController> logger,
            CpuCoreManager coreManager,
            CpuCoreConfigManager configManager,
            ICpuCoreManagerService managerService)
        {
            _logger = logger;
            _coreManager = coreManager;
            _configManager = configManager;
            _managerService = managerService;
        }

        /// <summary>
        /// 主页面
        /// </summary>
        public IActionResult Index()
        {
            // 检查管理员权限
            if (!PrivilegeManager.IsRunningAsAdministrator())
            {
                ViewBag.Error = "程序未能获得管理员权限！请确保以管理员身份启动程序。";
                ViewBag.Solution = "解决方案：右键点击程序图标，选择\"以管理员身份运行\"。";
                return View(new CpuCoreIndexViewModel());
            }

            var model = new CpuCoreIndexViewModel
            {
                Config = _coreManager.GetConfig(),
                Processes = _coreManager.GetCurrentProcesses(),
                RecentLogs = _coreManager.GetRecentLogs(50),
                SystemInfo = new SystemInfo
                {
                    ProcessorCount = Environment.ProcessorCount,
                    IsAdministrator = PrivilegeManager.IsRunningAsAdministrator(),
                    ConfigFilePath = _configManager.GetConfigFilePath(),
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    WorkingSet = Environment.WorkingSet,
                    TickCount = Environment.TickCount64
                }
            };

            return View(model);
        }

        #region API Endpoints

        /// <summary>
        /// 获取当前配置
        /// </summary>
        [HttpGet]
        [Route("api/cpucore/config")]
        public IActionResult GetConfig()
        {
            try
            {
                var config = _coreManager.GetConfig();
                return Json(new { success = true, data = config });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取配置失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        [HttpPost]
        [Route("api/cpucore/config")]
        public IActionResult UpdateConfig([FromBody] ProcessCoreConfig config)
        {
            try
            {
                if (!PrivilegeManager.IsRunningAsAdministrator())
                {
                    return Json(new { success = false, message = "需要管理员权限" });
                }

                _coreManager.UpdateConfig(config);
                bool saved = _configManager.SaveConfig(config);
                
                if (saved)
                {
                    _managerService.ReloadConfiguration();
                    return Json(new { success = true, message = "配置更新成功" });
                }
                else
                {
                    return Json(new { success = false, message = "配置保存失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新配置失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 验证和测试配置
        /// </summary>
        [HttpPost]
        [Route("api/cpucore/config/validate")]
        public IActionResult ValidateConfig([FromBody] ProcessCoreConfig config)
        {
            try
            {
                var validation = new
                {
                    isValid = true,
                    warnings = new List<string>(),
                    errors = new List<string>(),
                    suggestions = new List<string>()
                };

                // 验证默认核心数
                if (config.DefaultCoreCount <= 0 || config.DefaultCoreCount > Environment.ProcessorCount)
                {
                    validation.errors.Add($"默认核心数 {config.DefaultCoreCount} 无效，应在 1-{Environment.ProcessorCount} 之间");
                }

                // 验证进程名核心绑定
                foreach (var binding in config.ProcessCoreBindingMapping)
                {
                    var cores = binding.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim());
                    
                    foreach (var coreStr in cores)
                    {
                        if (!int.TryParse(coreStr, out int coreIndex) || 
                            coreIndex < 0 || coreIndex >= Environment.ProcessorCount)
                        {
                            validation.errors.Add($"进程 {binding.Key} 的核心索引 {coreStr} 无效，应在 0-{Environment.ProcessorCount - 1} 之间");
                        }
                    }
                }

                // 检查是否有进程同时配置了核心数和核心绑定
                var duplicateProcesses = config.ProcessNameMapping.Keys
                    .Intersect(config.ProcessCoreBindingMapping.Keys)
                    .ToList();

                if (duplicateProcesses.Any())
                {
                    validation.warnings.Add($"以下进程同时配置了核心数和核心绑定，将优先使用核心绑定: {string.Join(", ", duplicateProcesses)}");
                }

                // 性能建议
                if (config.ProcessCoreBindingMapping.Any())
                {
                    validation.suggestions.Add("建议为高优先级程序配置独立的CPU核心，避免与系统进程竞争");
                }

                if (config.ScanIntervalSeconds < 2)
                {
                    validation.warnings.Add("扫描间隔过短可能影响系统性能，建议设置为2秒或更长");
                }

                return Json(new { success = true, data = validation });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证配置失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 立即应用配置到所有匹配的进程
        /// </summary>
        [HttpPost]
        [Route("api/cpucore/config/apply")]
        public IActionResult ApplyConfigToAllProcesses()
        {
            try
            {
                if (!PrivilegeManager.IsRunningAsAdministrator())
                {
                    return Json(new { success = false, message = "需要管理员权限" });
                }

                var appliedCount = 0;
                var processes = _coreManager.GetCurrentProcesses();
                
                foreach (var process in processes)
                {
                    if (process.IsSystemCritical) continue;
                    
                    // 检查是否有配置需要应用
                    if (!string.IsNullOrEmpty(process.ConfiguredCoreBinding) || 
                        process.ConfiguredCoreCount != Environment.ProcessorCount)
                    {
                        if (!string.IsNullOrEmpty(process.ConfiguredCoreBinding))
                        {
                            var cores = process.ConfiguredCoreBinding.Split(',')
                                .Select(c => int.Parse(c.Trim())).ToList();
                            if (_coreManager.SetProcessCoreBinding(process.ProcessId, cores))
                            {
                                appliedCount++;
                            }
                        }
                        else if (process.ConfiguredCoreCount > 0)
                        {
                            if (_coreManager.SetProcessCoreCount(process.ProcessId, process.ConfiguredCoreCount))
                            {
                                appliedCount++;
                            }
                        }
                    }
                }

                return Json(new { 
                    success = true, 
                    message = $"配置应用完成，成功处理 {appliedCount} 个进程",
                    appliedCount = appliedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用配置失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取进程列表
        /// </summary>
        [HttpGet]
        [Route("api/cpucore/processes")]
        public IActionResult GetProcesses()
        {
            try
            {
                var processes = _coreManager.GetCurrentProcesses();
                return Json(new { success = true, data = processes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取进程列表失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 设置进程核心数
        /// </summary>
        [HttpPost]
        [Route("api/cpucore/process/{processId:int}/cores/{coreCount:int}")]
        public IActionResult SetProcessCores(int processId, int coreCount)
        {
            try
            {
                if (!PrivilegeManager.IsRunningAsAdministrator())
                {
                    return Json(new { success = false, message = "需要管理员权限" });
                }

                bool success = _coreManager.SetProcessCoreCount(processId, coreCount);
                return Json(new { 
                    success = success, 
                    message = success ? "设置成功" : "设置失败" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"设置进程 {processId} 核心数失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 设置进程具体核心绑定
        /// </summary>
        [HttpPost]
        [Route("api/cpucore/process/{processId:int}/binding")]
        public IActionResult SetProcessCoreBinding(int processId, [FromBody] CoreBindingRequest request)
        {
            try
            {
                if (!PrivilegeManager.IsRunningAsAdministrator())
                {
                    return Json(new { success = false, message = "需要管理员权限" });
                }

                if (request.CoreIndices == null || request.CoreIndices.Count == 0)
                {
                    return Json(new { success = false, message = "必须指定至少一个CPU核心" });
                }

                bool success = _coreManager.SetProcessCoreBinding(processId, request.CoreIndices);
                return Json(new { 
                    success = success, 
                    message = success ? $"核心绑定设置成功: {string.Join(",", request.CoreIndices)}" : "核心绑定设置失败" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"设置进程 {processId} 核心绑定失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 添加进程名核心绑定映射
        /// </summary>
        [HttpPost]
        [Route("api/cpucore/mapping/process/binding")]
        public IActionResult AddProcessCoreBindingMapping([FromBody] ProcessCoreBindingMappingRequest request)
        {
            try
            {
                if (!PrivilegeManager.IsRunningAsAdministrator())
                {
                    return Json(new { success = false, message = "需要管理员权限" });
                }

                if (string.IsNullOrWhiteSpace(request.ProcessName))
                {
                    return Json(new { success = false, message = "进程名不能为空" });
                }

                if (request.CoreIndices == null || request.CoreIndices.Count == 0)
                {
                    return Json(new { success = false, message = "必须指定至少一个CPU核心" });
                }

                var config = _coreManager.GetConfig();
                config.ProcessCoreBindingMapping[request.ProcessName] = string.Join(",", request.CoreIndices);
                
                _coreManager.UpdateConfig(config);
                bool saved = _configManager.SaveConfig(config);

                return Json(new { 
                    success = saved, 
                    message = saved ? $"核心绑定映射添加成功: {request.ProcessName} -> {string.Join(",", request.CoreIndices)}" : "保存配置失败" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加进程核心绑定映射失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除进程名核心绑定映射
        /// </summary>
        [HttpDelete]
        [Route("api/cpucore/mapping/process/binding/{processName}")]
        public IActionResult RemoveProcessCoreBindingMapping(string processName)
        {
            try
            {
                if (!PrivilegeManager.IsRunningAsAdministrator())
                {
                    return Json(new { success = false, message = "需要管理员权限" });
                }

                var config = _coreManager.GetConfig();
                bool removed = config.ProcessCoreBindingMapping.Remove(processName);
                
                if (removed)
                {
                    _coreManager.UpdateConfig(config);
                    bool saved = _configManager.SaveConfig(config);
                    return Json(new { 
                        success = saved, 
                        message = saved ? "核心绑定映射删除成功" : "保存配置失败" 
                    });
                }

                return Json(new { success = false, message = "核心绑定映射不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除进程核心绑定映射失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取可用的CPU核心列表
        /// </summary>
        [HttpGet]
        [Route("api/cpucore/available-cores")]
        public IActionResult GetAvailableCores()
        {
            try
            {
                var cores = new List<object>();
                for (int i = 0; i < Environment.ProcessorCount; i++)
                {
                    cores.Add(new { index = i, name = $"CPU 核心 {i}" });
                }

                return Json(new { 
                    success = true, 
                    data = new { 
                        totalCores = Environment.ProcessorCount,
                        cores = cores
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用CPU核心失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取操作日志
        /// </summary>
        [HttpGet]
        [Route("api/cpucore/logs")]
        public IActionResult GetLogs([FromQuery] int count = 100)
        {
            try
            {
                var logs = _coreManager.GetRecentLogs(count);
                return Json(new { success = true, data = logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取日志失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取系统信息
        /// </summary>
        [HttpGet]
        [Route("api/cpucore/system")]
        public IActionResult GetSystemInfo()
        {
            try
            {
                var info = new
                {
                    ProcessorCount = Environment.ProcessorCount,
                    IsAdministrator = PrivilegeManager.IsRunningAsAdministrator(),
                    ConfigFilePath = _configManager.GetConfigFilePath(),
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    WorkingSet = Environment.WorkingSet,
                    TickCount = Environment.TickCount64
                };

                return Json(new { success = true, data = info });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统信息失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 手动触发扫描
        /// </summary>
        [HttpPost]
        [Route("api/cpucore/scan")]
        public IActionResult TriggerScan()
        {
            try
            {
                if (!PrivilegeManager.IsRunningAsAdministrator())
                {
                    return Json(new { success = false, message = "需要管理员权限" });
                }

                _managerService.TriggerManualScan();
                return Json(new { success = true, message = "扫描已触发，正在应用配置到匹配的进程" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "触发扫描失败");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion
    }

    /// <summary>
    /// 进程名映射请求模型
    /// </summary>
    public class ProcessNameMappingRequest
    {
        public string ProcessName { get; set; } = string.Empty;
        public int CoreCount { get; set; }
    }

    /// <summary>
    /// 核心绑定请求模型
    /// </summary>
    public class CoreBindingRequest
    {
        public List<int> CoreIndices { get; set; } = new List<int>();
    }

    /// <summary>
    /// 进程核心绑定映射请求模型
    /// </summary>
    public class ProcessCoreBindingMappingRequest
    {
        public string ProcessName { get; set; } = string.Empty;
        public List<int> CoreIndices { get; set; } = new List<int>();
    }
}