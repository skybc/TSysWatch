using System.Text.Json;
using TSysWatch.Models;

namespace TSysWatch.Services
{
    /// <summary>
    /// 硬件监控配置管理器
    /// </summary>
    public class HardwareMonitorConfigManager
    {
        private readonly string _configFilePath;
        private readonly ILogger<HardwareMonitorConfigManager> _logger;
        private HardwareMonitorConfig _config = new();

        public HardwareMonitorConfigManager(ILogger<HardwareMonitorConfigManager> logger)
        {
            _logger = logger;
            var configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
            _configFilePath = Path.Combine(configDir, "HardwareMonitor.json");
            
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            // 初始化时立即加载配置
            _config = LoadConfig();
        }

        /// <summary>
        /// 读取配置
        /// </summary>
        public HardwareMonitorConfig LoadConfig()
        {
            try
            {
                _logger.LogInformation($"尝试加载配置文件: {_configFilePath}");
                
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    _logger.LogInformation($"配置文件内容: {json}");
                    
                    var loaded = JsonSerializer.Deserialize<HardwareMonitorConfig>(json);
                    if (loaded != null)
                    {
                        _config = loaded;
                        _config.ValidateConfig();
                        _logger.LogInformation($"配置加载成功: EnableTimedRecording={_config.EnableTimedRecording}, Interval={_config.RecordingIntervalSeconds}s");
                        return _config;
                    }
                }
                else
                {
                    _logger.LogWarning($"配置文件不存在: {_configFilePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"读取硬件监控配置失败: {ex.Message}, {ex.StackTrace}");
            }

            // 返回默认配置
            _logger.LogInformation("使用默认配置");
            _config = new HardwareMonitorConfig();
            SaveConfig(_config);
            return _config;
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public bool SaveConfig(HardwareMonitorConfig config)
        {
            try
            {
                config.ValidateConfig();
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);
                
                _logger.LogInformation($"保存配置到: {_configFilePath}");
                _logger.LogInformation($"配置内容: {json}");
                
                File.WriteAllText(_configFilePath, json);
                
                // 验证文件确实被写入
                if (File.Exists(_configFilePath))
                {
                    var savedContent = File.ReadAllText(_configFilePath);
                    _logger.LogInformation($"配置已成功保存, 文件大小: {savedContent.Length} 字节");
                }
                
                _config = config;
                _logger.LogInformation("硬件监控配置已保存");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"保存硬件监控配置失败: {ex.Message}, {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public HardwareMonitorConfig GetConfig()
        {
            if (_config == null)
            {
                _config = LoadConfig();
            }
            return _config;
        }
    }
}
