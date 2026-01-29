using System.Text.Json;
using TSysWatch.Models;

namespace TSysWatch.Services;

/// <summary>
/// 自更新系统服务接口
/// </summary>
public interface ISelfUpdateService
{
    /// <summary>
    /// 上传更新包
    /// </summary>
    Task<SelfUpdateResponse> UploadUpdatePackageAsync(IFormFile file);

    /// <summary>
    /// 触发自更新
    /// </summary>
    Task<SelfUpdateResponse> ApplyUpdateAsync();

    /// <summary>
    /// 获取最新的更新包版本信息
    /// </summary>
    Task<SelfUpdateResponse> GetLatestPackageInfoAsync();

    /// <summary>
    /// 清理过期的更新包
    /// </summary>
    Task<SelfUpdateResponse> CleanupOldPackagesAsync();
}

/// <summary>
/// 自更新系统服务实现
/// </summary>
public class SelfUpdateService : ISelfUpdateService
{
    private readonly ILogger<SelfUpdateService> _logger;
    private readonly SelfUpdateConfigManager _configManager;
    private readonly IWebHostEnvironment _environment;

    public SelfUpdateService(
        ILogger<SelfUpdateService> logger,
        SelfUpdateConfigManager configManager,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _configManager = configManager;
        _environment = environment;
    }

    /// <summary>
    /// 上传更新包
    /// </summary>
    public async Task<SelfUpdateResponse> UploadUpdatePackageAsync(IFormFile file)
    {
        try
        {
            var config = _configManager.GetConfig();

            if (!config.Enabled)
            {
                return new SelfUpdateResponse
                {
                    Success = false,
                    Message = "自更新功能已禁用",
                    Error = "自更新功能未启用"
                };
            }

            // 验证文件
            if (file == null || file.Length == 0)
            {
                return new SelfUpdateResponse
                {
                    Success = false,
                    Message = "文件不能为空",
                    Error = "上传的文件为空"
                };
            }

            // 验证文件类型
            var fileName = Path.GetFileName(file.FileName);
            if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return new SelfUpdateResponse
                {
                    Success = false,
                    Message = "只允许上传 .zip 文件",
                    Error = "不支持的文件类型"
                };
            }

            // 验证文件大小
            if (file.Length > config.MaxPackageSize)
            {
                return new SelfUpdateResponse
                {
                    Success = false,
                    Message = $"文件大小超过限制 ({FormatFileSize(config.MaxPackageSize)})",
                    Error = "文件过大"
                };
            }

            // 保存文件到配置的目录
            Directory.CreateDirectory(config.PackageDirectory!);
            var savePath = Path.Combine(config.PackageDirectory!, "update.zip");

            // 若已存在，先删除
            if (File.Exists(savePath))
            {
                try
                {
                    File.Delete(savePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除旧的更新包失败");
                }
            }

            // 保存新的更新包
            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 尝试读取版本信息
            var versionInfo = await TryReadVersionInfoAsync(savePath);

            var packageInfo = new PackageInfo
            {
                Version = versionInfo,
                PackagePath = savePath,
                PackageSize = file.Length,
                UploadTime = DateTime.Now
            };

            _logger.LogInformation(
                "更新包上传成功: {FileName}, 大小: {Size} 字节, 版本: {Version}",
                fileName,
                file.Length,
                versionInfo?.Version ?? "未知");

            return new SelfUpdateResponse
            {
                Success = true,
                Message = "更新包上传成功",
                PackageInfo = packageInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传更新包时出错");
            return new SelfUpdateResponse
            {
                Success = false,
                Message = "上传更新包失败",
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 触发自更新
    /// </summary>
    public async Task<SelfUpdateResponse> ApplyUpdateAsync()
    {
        try
        {
            var config = _configManager.GetConfig();

            if (!config.Enabled)
            {
                return new SelfUpdateResponse
                {
                    Success = false,
                    Message = "自更新功能已禁用",
                    Error = "自更新功能未启用"
                };
            }

            // 验证更新包文件存在
            var packagePath = Path.Combine(config.PackageDirectory!, "update.zip");
            if (!File.Exists(packagePath))
            {
                return new SelfUpdateResponse
                {
                    Success = false,
                    Message = "更新包文件不存在",
                    Error = "找不到 update.zip"
                };
            }

            // 验证 Updater.exe 存在
            if (string.IsNullOrEmpty(config.UpdaterExePath) || !File.Exists(config.UpdaterExePath))
            {
                return new SelfUpdateResponse
                {
                    Success = false,
                    Message = "Updater.exe 不存在",
                    Error = $"找不到 Updater.exe，路径: {config.UpdaterExePath}"
                };
            }

            var webRootPath = _environment.ContentRootPath;
            var backupPath = config.BackupDirectory;

            // 构建命令行参数
            var args = BuildUpdaterCommandLineArgs(packagePath, webRootPath, backupPath, config);

            _logger.LogInformation("启动 Updater.exe，参数: {Args}", args);

            // 启动 Updater.exe 进程
            var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = config.UpdaterExePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
                // 重要：以管理员身份运行
                Verb = "runas"
            };

            try
            {
                process.Start();

                _logger.LogInformation(
                    "Updater.exe 已启动，PID: {ProcessId}",
                    process.Id);

                return new SelfUpdateResponse
                {
                    Success = true,
                    Message = "更新已开始，系统将在更新完成后重启",
                    PackageInfo = new PackageInfo
                    {
                        PackagePath = packagePath,
                        PackageSize = new FileInfo(packagePath).Length
                    }
                };
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                _logger.LogError(ex, "启动 Updater.exe 失败，可能需要管理员权限");
                return new SelfUpdateResponse
                {
                    Success = false,
                    Message = "启动更新程序失败，可能需要管理员权限",
                    Error = ex.Message
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发自更新时出错");
            return new SelfUpdateResponse
            {
                Success = false,
                Message = "触发更新失败",
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 获取最新的更新包版本信息
    /// </summary>
    public async Task<SelfUpdateResponse> GetLatestPackageInfoAsync()
    {
        try
        {
            var config = _configManager.GetConfig();
            var packagePath = Path.Combine(config.PackageDirectory!, "update.zip");

            if (!File.Exists(packagePath))
            {
                return new SelfUpdateResponse
                {
                    Success = false,
                    Message = "没有可用的更新包",
                    Error = "找不到 update.zip"
                };
            }

            var versionInfo = await TryReadVersionInfoAsync(packagePath);
            var fileInfo = new FileInfo(packagePath);

            return new SelfUpdateResponse
            {
                Success = true,
                Message = "获取更新包信息成功",
                PackageInfo = new PackageInfo
                {
                    Version = versionInfo,
                    PackagePath = packagePath,
                    PackageSize = fileInfo.Length,
                    UploadTime = fileInfo.LastWriteTime
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取更新包信息时出错");
            return new SelfUpdateResponse
            {
                Success = false,
                Message = "获取更新包信息失败",
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 清理过期的更新包
    /// </summary>
    public async Task<SelfUpdateResponse> CleanupOldPackagesAsync()
    {
        try
        {
            var config = _configManager.GetConfig();
            var packageDir = config.PackageDirectory;

            if (!Directory.Exists(packageDir))
            {
                return new SelfUpdateResponse
                {
                    Success = true,
                    Message = "没有过期的更新包"
                };
            }

            var files = new DirectoryInfo(packageDir).GetFiles("*.zip");
            int deletedCount = 0;

            // 保留最新的 3 个包，删除其余的
            foreach (var file in files.OrderByDescending(f => f.LastWriteTime).Skip(3))
            {
                try
                {
                    file.Delete();
                    deletedCount++;
                    _logger.LogInformation("删除过期更新包: {FileName}", file.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除过期更新包失败: {FileName}", file.Name);
                }
            }

            return new SelfUpdateResponse
            {
                Success = true,
                Message = $"清理完成，删除了 {deletedCount} 个过期包"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期更新包时出错");
            return new SelfUpdateResponse
            {
                Success = false,
                Message = "清理过期包失败",
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 尝试从 ZIP 包中读取版本信息
    /// </summary>
    private async Task<VersionInfo?> TryReadVersionInfoAsync(string zipPath)
    {
        try
        {
            using (var archive = System.IO.Compression.ZipFile.OpenRead(zipPath))
            {
                var versionEntry = archive.GetEntry("version.json");
                if (versionEntry != null)
                {
                    using (var stream = versionEntry.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        var json = await reader.ReadToEndAsync();
                        return JsonSerializer.Deserialize<VersionInfo>(json);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取更新包版本信息失败");
        }

        return null;
    }

    /// <summary>
    /// 构建 Updater.exe 命令行参数
    /// </summary>
    private string BuildUpdaterCommandLineArgs(
        string packagePath,
        string webRootPath,
        string backupPath,
        SelfUpdateConfig config)
    {
        var args = new List<string>
        {
            $"--package-path \"{packagePath}\"",
            $"--web-root \"{webRootPath}\"",
            $"--backup-path \"{backupPath}\"",
            $"--hosting-type {config.HostingType}",
            $"--timeout {config.UpdateTimeoutMs}"
        };

        if (!string.IsNullOrEmpty(config.IisAppPoolName))
            args.Add($"--iis-apppool \"{config.IisAppPoolName}\"");

        if (!string.IsNullOrEmpty(config.IisSiteName))
            args.Add($"--iis-site \"{config.IisSiteName}\"");

        if (!string.IsNullOrEmpty(config.WindowsServiceName))
            args.Add($"--service-name \"{config.WindowsServiceName}\"");

        if (!string.IsNullOrEmpty(config.KestrelProcessName))
            args.Add($"--process-name {config.KestrelProcessName}");

        return string.Join(" ", args);
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
