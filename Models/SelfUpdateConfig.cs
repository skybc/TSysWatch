namespace TSysWatch.Models;

/// <summary>
/// 自更新系统配置模型
/// </summary>
public class SelfUpdateConfig
{
    /// <summary>
    /// 是否启用自更新功能
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 更新包存储目录
    /// </summary>
    public string? PackageDirectory { get; set; }

    /// <summary>
    /// 备份目录
    /// </summary>
    public string? BackupDirectory { get; set; }

    /// <summary>
    /// Updater.exe 路径
    /// </summary>
    public string? UpdaterExePath { get; set; }

    /// <summary>
    /// Web 宿主类型 (IIS / WindowsService / Kestrel)
    /// </summary>
    public string? HostingType { get; set; } = "Kestrel";

    /// <summary>
    /// IIS 应用池名称 (仅 IIS 需要)
    /// </summary>
    public string? IisAppPoolName { get; set; }

    /// <summary>
    /// IIS 网站名称 (仅 IIS 需要)
    /// </summary>
    public string? IisSiteName { get; set; }

    /// <summary>
    /// Windows Service 名称 (仅 Windows Service 需要)
    /// </summary>
    public string? WindowsServiceName { get; set; }

    /// <summary>
    /// Kestrel 进程名称
    /// </summary>
    public string? KestrelProcessName { get; set; } = "dotnet";

    /// <summary>
    /// 最大更新包文件大小 (字节)
    /// </summary>
    public long MaxPackageSize { get; set; } = 500 * 1024 * 1024; // 500 MB

    /// <summary>
    /// 更新超时时间 (毫秒)
    /// </summary>
    public int UpdateTimeoutMs { get; set; } = 5 * 60 * 1000; // 5 minutes
}
