using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using TSysWatch;
using TSysWatch.Services;

internal partial class Program
{
    private static void Main(string[] args)
    {
        AutoDeleteFile.Start();
        AutoCopyFile.Start();
        AutoMoveFile.Start();
        
        // 获取可用端口
        var portDetectionService = new PortDetectionService();
        int availablePort = portDetectionService.GetAvailablePort();
        
        var builder = WebApplication.CreateBuilder(args).Inject();
        
        // 配置动态端口
        builder.WebHost.UseUrls($"http://*:{availablePort}");
        builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation().AddInject();

        // 注册 CPU 核心管理器服务
        builder.Services.AddSingleton<CpuCoreManager>();
        builder.Services.AddSingleton<CpuCoreConfigManager>();
        builder.Services.AddSingleton<ICpuCoreManagerService, CpuCoreManagerServiceWrapper>();
        builder.Services.AddHostedService<CpuCoreManagerService>();

        // 注册硬件监控服务
        builder.Services.AddSingleton<HardwareMonitorConfigManager>();
        builder.Services.AddSingleton<HardwareDataCollectionService>();
        builder.Services.AddHostedService<HardwareDataRecordingService>();

        // 注册自更新系统服务
        builder.Services.AddSingleton<SelfUpdateConfigManager>();
        builder.Services.AddScoped<ISelfUpdateService, SelfUpdateService>();

        var app = builder.Build().UseDefaultServiceProvider();
        
        app.UseStaticFiles();
        app.UseAuthorization();
        app.UseInject();
        app.MapDefaultControllerRoute();
        app.MapControllers();
        app.Run();
    } 
}
