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
        MonitorWindow monitorWindow = new MonitorWindow();
        DbRepair.Start();
        AutoDeleteFile.Start();
        AutoCopyFile.Start();
        AutoMoveFile.Start();
        Task.Run(monitorWindow.RunMonitor);
        var builder = WebApplication.CreateBuilder(args).Inject();
        builder.Services.AddControllersWithViews().AddInject();
        var app = builder.Build().UseDefaultServiceProvider();
        app.UseStaticFiles();
        app.UseAuthorization();
        app.UseInject();
        app.MapDefaultControllerRoute();
        app.MapControllers();
        app.Run();
    }




}
