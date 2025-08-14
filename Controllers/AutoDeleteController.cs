using Microsoft.AspNetCore.Mvc;
using TSysWatch;

namespace TSysWatch.Controllers
{
    /// <summary>
    /// 自动删除文件管理控制器
    /// </summary>
    public class AutoDeleteController : Controller
    {
        /// <summary>
        /// 自动删除文件配置管理页面
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var configs = AutoDeleteFileManager.GetCurrentConfigs();
            var drives = AutoDeleteFileManager.GetDriveInfos();

            ViewBag.Drives = drives;
            ViewBag.Configs = configs;

            return View(configs);
        }

        /// <summary>
        /// 获取当前配置信息的API
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetConfigs()
        {
            try
            {
                var configs = AutoDeleteFileManager.GetCurrentConfigs();
                var drives = AutoDeleteFileManager.GetDriveInfos();

                var result = configs.Select(config => new
                {
                    DriveLetter = config.DriveLetter,
                    DeleteDirectories = config.DeleteDirectories,
                    StartDeleteSizeGB = config.StartDeleteSizeGB,
                    StopDeleteSizeGB = config.StopDeleteSizeGB,
                    StartDeleteFileDays = config.StartDeleteFileDays,
                    LogicMode = config.LogicMode.ToString(),
                    LogicModeDescription = GetLogicModeDescription(config.LogicMode)
                }).ToList();

                var driveInfos = drives.Select(drive => new
                {
                    DriveLetter = drive.Name.TrimEnd('\\'),
                    TotalSize = AutoDeleteFileManager.FormatBytes(drive.TotalSize),
                    FreeSpace = AutoDeleteFileManager.FormatBytes(drive.AvailableFreeSpace),
                    FreeSpaceGB = Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2)
                }).ToList();

                return Json(new { success = true, configs = result, drives = driveInfos });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 测试删除条件API
        /// </summary>
        /// <param name="driveLetter">驱动器字母</param>
        /// <param name="filePath">测试文件路径</param>
        /// <param name="startDeleteSizeGB">容量阈值</param>
        /// <param name="startDeleteFileDays">时间阈值</param>
        /// <param name="logicMode">逻辑模式</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult TestDeleteCondition([FromBody] TestConditionRequest request)
        {
            try
            {
                if (!System.IO.File.Exists(request.FilePath))
                {
                    return Json(new { success = false, message = "文件不存在" });
                }

                var driveInfo = new DriveInfo(request.DriveLetter);
                double currentFreeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

                var config = new DiskCleanupConfig
                {
                    DriveLetter = request.DriveLetter,
                    StartDeleteSizeGB = request.StartDeleteSizeGB,
                    StartDeleteFileDays = request.StartDeleteFileDays,
                    LogicMode = Enum.Parse<DeleteLogicMode>(request.LogicMode, true)
                };
                // 
                FileInfo fileInfo = new FileInfo(request.FilePath);
                var deleteReason = AutoDeleteFileManager.ShouldDeleteFile(config, currentFreeSpaceGB, fileInfo);
                var shouldDelete = deleteReason.CanDelete;

                // var fileInfo = new FileInfo(request.FilePath);
                var fileAge = DateTime.Now - (fileInfo.CreationTime > fileInfo.LastWriteTime ? fileInfo.CreationTime : fileInfo.LastWriteTime);

                // 检查各个条件
                bool capacityCondition = currentFreeSpaceGB < request.StartDeleteSizeGB;
                bool timeCondition = AutoDeleteFileManager.IsFileOlderThanDays(fileInfo, request.StartDeleteFileDays);

                return Json(new
                {
                    success = true,
                    shouldDelete = shouldDelete,
                    currentFreeSpaceGB = Math.Round(currentFreeSpaceGB, 2),
                    fileAge = fileAge.Days,
                    capacityCondition = capacityCondition,
                    timeCondition = timeCondition,
                    logicMode = request.LogicMode,
                    explanation = GetDeleteExplanation(capacityCondition, timeCondition, config.LogicMode, shouldDelete)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GetLogicModeDescription(DeleteLogicMode mode)
        {
            return mode switch
            {
                DeleteLogicMode.AND => "且 - 必须同时满足容量和时间条件",
                DeleteLogicMode.OR => "或 - 满足容量或时间条件之一即可",
                _ => "未知"
            };
        }

        private string GetDeleteExplanation(bool capacityCondition, bool timeCondition, DeleteLogicMode logicMode, bool result)
        {
            string explanation = $"容量条件：{(capacityCondition ? "满足" : "不满足")}，时间条件：{(timeCondition ? "满足" : "不满足")}。";

            if (logicMode == DeleteLogicMode.AND)
            {
                explanation += $" 使用AND逻辑，需同时满足两个条件，结果：{(result ? "删除" : "不删除")}";
            }
            else
            {
                explanation += $" 使用OR逻辑，满足任一条件即可，结果：{(result ? "删除" : "不删除")}";
            }

            return explanation;
        }
    }

    /// <summary>
    /// 测试条件请求模型
    /// </summary>
    public class TestConditionRequest
    {
        public string DriveLetter { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public double StartDeleteSizeGB { get; set; }
        public int StartDeleteFileDays { get; set; }
        public string LogicMode { get; set; } = "OR";
    }
}
