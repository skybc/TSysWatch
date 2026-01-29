namespace TSysWatch.Services
{
    /// <summary>
    /// 硬件数据定时采集后台服务
    /// </summary>
    public class HardwareDataRecordingService : BackgroundService
    {
        private readonly HardwareDataCollectionService _collectionService;
        private readonly HardwareMonitorConfigManager _configManager;
        private readonly ILogger<HardwareDataRecordingService> _logger;
        private CancellationTokenSource? _cancellationTokenSource;

        public HardwareDataRecordingService(
            HardwareDataCollectionService collectionService,
            HardwareMonitorConfigManager configManager,
            ILogger<HardwareDataRecordingService> logger)
        {
            _collectionService = collectionService;
            _configManager = configManager;
            _logger = logger;
        }

        /// <summary>
        /// 执行背景服务
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var config = _configManager.GetConfig();

                    // 检查是否启用定时记录
                    if (!config.EnableTimedRecording)
                    {
                        // 如果未启用，每10秒检查一次配置
                        await Task.Delay(10000, stoppingToken);
                        continue;
                    }

                    try
                    {
                        // 采集硬件数据
                        var data = _collectionService.CollectHardwareData();
                        
                        // 保存到CSV
                        if (data.Count > 0)
                        {
                            _collectionService.SaveToCsv(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"采集硬件数据时出错: {ex.Message}");
                    }

                    // 等待配置的时间间隔后继续
                    var delayMs = Math.Max(config.RecordingIntervalSeconds * 1000, 2000);
                    await Task.Delay(delayMs, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("硬件数据采集服务已停止");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
            }
        }
    }
}
