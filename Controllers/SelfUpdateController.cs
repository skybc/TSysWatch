using Microsoft.AspNetCore.Mvc;
using TSysWatch.Models;
using TSysWatch.Services;

namespace TSysWatch.Controllers;

/// <summary>
/// 自更新系统控制器
/// 同时提供 MVC 视图和 API 接口
/// </summary>
public class SelfUpdateController : Controller
{
    private readonly ISelfUpdateService _selfUpdateService;
    private readonly ILogger<SelfUpdateController> _logger;

    public SelfUpdateController(
        ISelfUpdateService selfUpdateService,
        ILogger<SelfUpdateController> logger)
    {
        _selfUpdateService = selfUpdateService;
        _logger = logger;
    }

    /// <summary>
    /// 显示自更新管理页面
    /// </summary>
    /// <returns>自更新页面视图</returns>
    [HttpGet("/self-update")]
    public IActionResult Index()
    {
        _logger.LogInformation("访问自更新管理页面");

        var model = new SelfUpdatePageViewModel
        {
            CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0",
            BuildTime = System.IO.File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location),
            AppType = "aspnetcore",
            RunningPath = AppDomain.CurrentDomain.BaseDirectory,
            MaxUploadSizeMB = 500
        };

        return View(model);
    }

    /// <summary>
    /// 上传更新包文件
    /// </summary>
    /// <remarks>
    /// 接受 multipart/form-data 格式的 ZIP 文件上传
    /// 
    /// 示例请求:
    /// POST /api/self-update/upload
    /// Content-Type: multipart/form-data
    /// 
    /// 文件: update.zip (限制: 必须为 .zip，最大 500MB)
    /// </remarks>
    /// <param name="file">要上传的 ZIP 文件</param>
    /// <returns>上传结果</returns>
    [HttpPost("/api/self-update/upload")]
    [ProducesResponseType(typeof(SelfUpdateResponse), 200)]
    [ProducesResponseType(typeof(SelfUpdateResponse), 400)]
    [ProducesResponseType(typeof(SelfUpdateResponse), 500)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        _logger.LogInformation("接收到更新包上传请求，文件: {FileName}, 大小: {Size}", file?.FileName, file?.Length);

        var result = await _selfUpdateService.UploadUpdatePackageAsync(file);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 触发自更新程序
    /// </summary>
    /// <remarks>
    /// 触发 Updater.exe 启动更新过程
    /// 
    /// 重要: 此接口仅启动更新程序，实际更新由 Updater.exe 执行
    /// - 需要管理员权限
    /// - 需要已上传更新包
    /// - Web 程序在启动Updater后会返回'更新已开始'
    /// 
    /// 示例请求:
    /// POST /api/self-update/apply
    /// </remarks>
    /// <returns>触发结果</returns>
    [HttpPost("/api/self-update/apply")]
    [ProducesResponseType(typeof(SelfUpdateResponse), 200)]
    [ProducesResponseType(typeof(SelfUpdateResponse), 400)]
    [ProducesResponseType(typeof(SelfUpdateResponse), 500)]
    public async Task<IActionResult> Apply()
    {
        _logger.LogInformation("接收到更新触发请求");

        var result = await _selfUpdateService.ApplyUpdateAsync();

        if (!result.Success)
        {
            return BadRequest(result);
        }

        // 异步等待片刻后优雅退出（可选）
        // 如果需要立即返回，则不需要退出
        _ = Task.Delay(1000).ContinueWith(_ =>
        {
            _logger.LogInformation("Updater.exe 已启动，Web 即将退出以允许更新");
            // 可选：优雅地关闭 Web 应用
            // Environment.Exit(0);
        });

        return Ok(result);
    }

    /// <summary>
    /// 获取最新的更新包信息
    /// </summary>
    /// <remarks>
    /// 获取当前已上传的更新包的版本信息
    /// 
    /// 示例请求:
    /// GET /api/self-update/package-info
    /// </remarks>
    /// <returns>更新包信息</returns>
    [HttpGet("/api/self-update/package-info")]
    [ProducesResponseType(typeof(SelfUpdateResponse), 200)]
    [ProducesResponseType(typeof(SelfUpdateResponse), 404)]
    [ProducesResponseType(typeof(SelfUpdateResponse), 500)]
    public async Task<IActionResult> GetPackageInfo()
    {
        _logger.LogInformation("接收到获取更新包信息请求");

        var result = await _selfUpdateService.GetLatestPackageInfoAsync();

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 清理过期的更新包
    /// </summary>
    /// <remarks>
    /// 删除保留数量之外的历史更新包文件
    /// （保留最新的 3 个包）
    /// 
    /// 示例请求:
    /// POST /api/self-update/cleanup
    /// </remarks>
    /// <returns>清理结果</returns>
    [HttpPost("/api/self-update/cleanup")]
    [ProducesResponseType(typeof(SelfUpdateResponse), 200)]
    [ProducesResponseType(typeof(SelfUpdateResponse), 500)]
    public async Task<IActionResult> Cleanup()
    {
        _logger.LogInformation("接收到清理过期更新包请求");

        var result = await _selfUpdateService.CleanupOldPackagesAsync();

        return Ok(result);
    }

    /// <summary>
    /// 健康检查接口
    /// </summary>
    /// <remarks>
    /// 用于检查更新系统是否可用
    /// 
    /// 示例请求:
    /// GET /api/self-update/health
    /// </remarks>
    /// <returns>健康状态</returns>
    [HttpGet("/api/self-update/health")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            message = "自更新系统正常运行"
        });
    }
}
