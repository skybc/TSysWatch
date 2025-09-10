using TSysWatch.Services.Models;

namespace TSysWatch.Services
{
    /// <summary>
    /// CPU 核心管理器后台服务
    /// </summary>
    public class CpuCoreManagerService : BackgroundService
    {
        private readonly ILogger<CpuCoreManagerService> _logger;
        private readonly CpuCoreManager _coreManager;
        private readonly CpuCoreConfigManager _configManager;
        private Timer? _scanTimer;
        private readonly object _timerLock = new object();

        public CpuCoreManagerService(
            ILogger<CpuCoreManagerService> logger,
            CpuCoreManager coreManager,
            CpuCoreConfigManager configManager)
        {
            _logger = logger;
            _coreManager = coreManager;
            _configManager = configManager;
            
            // 设置静态引用
            CpuCoreManagerServiceWrapper.SetBackgroundService(this);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CPU 核心管理器服务启动");

            // 由于使用了 app.manifest 请求管理员权限，这里检查是否成功获得权限
            if (!PrivilegeManager.IsRunningAsAdministrator())
            {
                _logger.LogError("程序未能获得管理员权限，请确保以管理员身份运行！");
                // 不返回，继续尝试启用调试权限
            }
            else
            {
                _logger.LogInformation("管理员权限验证成功");
            }

            // 启用调试权限
            if (!PrivilegeManager.EnableDebugPrivilege())
            {
                _logger.LogWarning("无法启用调试权限，可能影响某些进程的操作");
            }
            else
            {
                _logger.LogInformation("调试权限启用成功");
            }

            // 加载配置
            var config = _configManager.LoadConfig();
            _coreManager.UpdateConfig(config);
            
            // 详细记录配置加载情况
            _logger.LogInformation($"配置加载完成:");
            _logger.LogInformation($"  - 默认核心数: {config.DefaultCoreCount}");
            _logger.LogInformation($"  - 扫描间隔: {config.ScanIntervalSeconds}秒");
            _logger.LogInformation($"  - 功能启用: {config.Enabled}");
            _logger.LogInformation($"  - 进程名核心数映射: {config.ProcessNameMapping.Count} 项");
            _logger.LogInformation($"  - PID核心数映射: {config.PidMapping.Count} 项");
            _logger.LogInformation($"  - 进程名核心绑定映射: {config.ProcessCoreBindingMapping.Count} 项");
            _logger.LogInformation($"  - 系统关键进程: {config.CriticalProcesses.Count} 个");
            
            // 记录核心绑定映射详情
            if (config.ProcessCoreBindingMapping.Any())
            {
                _logger.LogInformation("进程名核心绑定映射详情:");
                foreach (var kvp in config.ProcessCoreBindingMapping)
                {
                    _logger.LogInformation($"  - {kvp.Key} -> 核心 {kvp.Value}");
                }
            }
            
          

            // 启动定时扫描
            StartPeriodicScan();

            // 保持服务运行
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CPU 核心管理器服务停止");
            
            lock (_timerLock)
            {
                _scanTimer?.Dispose();
                _scanTimer = null;
            }
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// 启动定时扫描
        /// </summary>
        private void StartPeriodicScan()
        {
            var config = _coreManager.GetConfig();
            var interval = TimeSpan.FromSeconds(config.ScanIntervalSeconds);

            lock (_timerLock)
            {
                _scanTimer?.Dispose();
                _scanTimer = new Timer(OnScanTimer, null, TimeSpan.Zero, interval);
            }
            
            _logger.LogInformation($"定时扫描已启动，间隔: {config.ScanIntervalSeconds}秒");
        }

        /// <summary>
        /// 扫描定时器回调
        /// </summary>
        private void OnScanTimer(object? state)
        {
            try
            {
                _coreManager.ScanAndApplyConfigurations();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时扫描过程中发生错误");
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void ReloadConfiguration()
        {
            try
            {
                _logger.LogInformation("接收到配置重载请求");
                StartPeriodicScan(); // 重启定时器以应用新的扫描间隔
                _logger.LogInformation("配置重载处理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理配置重载请求时发生错误");
            }
        }

        /// <summary>
        /// 手动触发扫描
        /// </summary>
        public void TriggerManualScan()
        {
            Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("接收到手动扫描请求，开始执行");
                    _coreManager.ScanAndApplyConfigurations();
                    _logger.LogInformation("手动扫描执行完成");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "执行手动扫描时发生错误");
                }
            });
        }
    }
}