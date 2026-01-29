namespace TSysWatch.Models;

/// <summary>
/// 版本信息模型
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// 版本号
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 构建时间
    /// </summary>
    public string? BuildTime { get; set; }

    /// <summary>
    /// 应用类型
    /// </summary>
    public string? AppType { get; set; }

    /// <summary>
    /// 更新说明/发布说明
    /// </summary>
    public string? ReleaseNotes { get; set; }
}
