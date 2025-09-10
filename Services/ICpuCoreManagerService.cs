using TSysWatch.Services.Models;

namespace TSysWatch.Services
{
    /// <summary>
    /// CPU 核心管理器服务接口
    /// </summary>
    public interface ICpuCoreManagerService
    {
        /// <summary>
        /// 重新加载配置
        /// </summary>
        void ReloadConfiguration();

        /// <summary>
        /// 手动触发扫描
        /// </summary>
        void TriggerManualScan();
    }

    /// <summary>
    /// CPU 核心管理器服务包装器
    /// </summary>
    public class CpuCoreManagerServiceWrapper : ICpuCoreManagerService
    {
        private readonly CpuCoreManager _coreManager;
        private readonly CpuCoreConfigManager _configManager;
        private readonly ILogger<CpuCoreManagerServiceWrapper> _logger;
        private static readonly object _staticLock = new object();
        private static CpuCoreManagerService? _backgroundService;

        public CpuCoreManagerServiceWrapper(
            CpuCoreManager coreManager,
            CpuCoreConfigManager configManager,
            ILogger<CpuCoreManagerServiceWrapper> logger)
        {
            _coreManager = coreManager;
            _configManager = configManager;
            _logger = logger;
        }

        /// <summary>
        /// 设置后台服务引用
        /// </summary>
        internal static void SetBackgroundService(CpuCoreManagerService backgroundService)
        {
            lock (_staticLock)
            {
                _backgroundService = backgroundService;
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void ReloadConfiguration()
        {
            try
            {
                var config = _configManager.LoadConfig();
                _coreManager.UpdateConfig(config);
                
                // 通知后台服务重新启动定时器
                lock (_staticLock)
                {
                    _backgroundService?.ReloadConfiguration();
                }
                
                _logger.LogInformation("配置重新加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新加载配置时发生错误");
            }
        }

        /// <summary>
        /// 手动触发扫描
        /// </summary>
        public void TriggerManualScan()
        {
            try
            {
                _logger.LogInformation("手动触发扫描请求");
                
                // 通知后台服务执行扫描
                lock (_staticLock)
                {
                    _backgroundService?.TriggerManualScan();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手动扫描请求时发生错误");
            }
        }
    }
}