using System.Text.Json;
using TSysWatch.Models;

namespace TSysWatch.Services;

/// <summary>
/// 自更新系统配置管理器
/// </summary>
public class SelfUpdateConfigManager
{
    private readonly ILogger<SelfUpdateConfigManager> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _configPath;
    private SelfUpdateConfig _config = new();

    /// <summary>
    /// 配置文件名称
    /// </summary>
    private const string CONFIG_FILE = "SelfUpdate.json";

    public SelfUpdateConfigManager(ILogger<SelfUpdateConfigManager> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;

        var iniConfigDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ini_config");
        _configPath = Path.Combine(iniConfigDir, CONFIG_FILE);

        LoadConfig();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<SelfUpdateConfig>(json) ?? new SelfUpdateConfig();
                _logger.LogInformation("自更新配置加载成功: {ConfigPath}", _configPath);
            }
            else
            {
                _logger.LogWarning("配置文件不存在，使用默认配置: {ConfigPath}", _configPath);
                InitializeDefaultConfig();
                SaveConfig();
            }

            ValidateConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载自更新配置失败");
            InitializeDefaultConfig();
        }
    }

    /// <summary>
    /// 初始化默认配置
    /// </summary>
    private void InitializeDefaultConfig()
    {
        var appRoot = _environment.ContentRootPath;

        _config = new SelfUpdateConfig
        {
            Enabled = true,
            PackageDirectory = Path.Combine(appRoot, "..", "WebUpdater", "packages"),
            BackupDirectory = Path.Combine(appRoot, "..", "WebUpdater", "backup"),
            HostingType = "Kestrel",
            KestrelProcessName = "dotnet",
            MaxPackageSize = 500 * 1024 * 1024,
            UpdateTimeoutMs = 5 * 60 * 1000
        };

        // 尝试检测 Updater.exe 位置
        var updaterPath = Path.Combine(appRoot, "..", "Updater", "Updater.exe");
        if (File.Exists(updaterPath))
        {
            _config.UpdaterExePath = updaterPath;
        }

        _logger.LogInformation("默认配置已初始化");
    }

    /// <summary>
    /// 验证配置
    /// </summary>
    private void ValidateConfig()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(_config.PackageDirectory))
            errors.Add("更新包目录未配置");

        if (string.IsNullOrEmpty(_config.BackupDirectory))
            errors.Add("备份目录未配置");

        if (string.IsNullOrEmpty(_config.HostingType))
            errors.Add("宿主类型未配置");

        if (_config.MaxPackageSize <= 0)
            errors.Add("最大包大小配置无效");

        if (errors.Count > 0)
        {
            _logger.LogWarning("配置验证发现问题: {Errors}", string.Join("; ", errors));
        }

        // 创建必要的目录
        try
        {
            Directory.CreateDirectory(_config.PackageDirectory!);
            Directory.CreateDirectory(_config.BackupDirectory!);
            _logger.LogInformation("更新所需目录已确保存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建必要目录失败");
        }
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    private void SaveConfig()
    {
        try
        {
            var iniConfigDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ini_config");
            Directory.CreateDirectory(iniConfigDir);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_config, options);
            File.WriteAllText(_configPath, json);

            _logger.LogInformation("自更新配置已保存: {ConfigPath}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存自更新配置失败");
        }
    }

    /// <summary>
    /// 获取配置
    /// </summary>
    public SelfUpdateConfig GetConfig() => _config;

    /// <summary>
    /// 更新配置
    /// </summary>
    public void UpdateConfig(SelfUpdateConfig config)
    {
        _config = config;
        SaveConfig();
        ValidateConfig();
        _logger.LogInformation("自更新配置已更新");
    }
}
