using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
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

            var configDisplays = configs.Select(config => new DiskCleanupConfigDisplay
            {
                DriveLetter = config.DriveLetter,
                DeleteDirectories = config.DeleteDirectories,
                StartDeleteSizeGB = config.StartDeleteSizeGB,
                StopDeleteSizeGB = config.StopDeleteSizeGB,
                StartDeleteFileDays = config.StartDeleteFileDays,
                LogicMode = config.LogicMode.ToString()
            }).ToList();

            var driveInfos = drives.Select(drive => new DriveInfoDisplay
            {
                DriveLetter = drive.Name.TrimEnd('\\'),
                TotalSize = AutoDeleteFileManager.FormatBytes(drive.TotalSize),
                FreeSpace = AutoDeleteFileManager.FormatBytes(drive.AvailableFreeSpace),
                FreeSpaceGB = Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2)
            }).ToList();

            var viewModel = new AutoDeleteFilePageViewModel
            {
                Configs = configDisplays,
                Drives = driveInfos
            };

            return View(viewModel);
        }

        /// <summary>
        /// 获取配置列表的HTML（Partial View）
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetConfigsHtml()
        {
            try
            {
                var configs = AutoDeleteFileManager.GetCurrentConfigs();

                var configDisplays = configs.Select(config => new DiskCleanupConfigDisplay
                {
                    DriveLetter = config.DriveLetter,
                    DeleteDirectories = config.DeleteDirectories,
                    StartDeleteSizeGB = config.StartDeleteSizeGB,
                    StopDeleteSizeGB = config.StopDeleteSizeGB,
                    StartDeleteFileDays = config.StartDeleteFileDays,
                    LogicMode = config.LogicMode.ToString()
                }).ToList();

                return PartialView("_ConfigList", configDisplays);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 获取驱动器列表的HTML（Partial View）
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetDrivesHtml()
        {
            try
            {
                var drives = AutoDeleteFileManager.GetDriveInfos();

                var driveInfos = drives.Select(drive => new DriveInfoDisplay
                {
                    DriveLetter = drive.Name.TrimEnd('\\'),
                    TotalSize = AutoDeleteFileManager.FormatBytes(drive.TotalSize),
                    FreeSpace = AutoDeleteFileManager.FormatBytes(drive.AvailableFreeSpace),
                    FreeSpaceGB = Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2)
                }).ToList();

                return PartialView("_DrivesList", driveInfos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
        /// 添加或更新配置API
        /// </summary>
        /// <param name="request">配置请求</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult SaveConfig([FromBody] SaveDiskConfigRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.DriveLetter))
                {
                    return Json(new { success = false, message = "驱动器字母不能为空" });
                }

                if (request.DeleteDirectories == null || request.DeleteDirectories.Count == 0)
                {
                    return Json(new { success = false, message = "删除目录不能为空" });
                }

                if (!Enum.TryParse<DeleteLogicMode>(request.LogicMode, true, out DeleteLogicMode logicMode))
                {
                    logicMode = DeleteLogicMode.OR;
                }

                AutoDeleteFileManager.AddOrUpdateConfig(
                    request.DriveLetter,
                    request.DeleteDirectories,
                    request.StartDeleteSizeGB,
                    request.StopDeleteSizeGB,
                    request.StartDeleteFileDays,
                    logicMode
                );

                return Json(new { success = true, message = "配置保存成功" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除配置API
        /// </summary>
        /// <param name="driveLetter">驱动器字母</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult DeleteConfig([FromBody] DeleteDiskConfigRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.DriveLetter))
                {
                    return Json(new { success = false, message = "驱动器字母不能为空" });
                }

                AutoDeleteFileManager.RemoveConfig(request.DriveLetter);
                return Json(new { success = true, message = "配置删除成功" });
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

        /// <summary>
        /// 获取删除记录CSV文件列表
        /// </summary>
        [HttpGet]
        public IActionResult GetDeleteRecordFiles(string startDate = null, string endDate = null)
        {
            try
            {
                string recordDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "record", "AutoDeleteFile");
                if (!Directory.Exists(recordDir))
                {
                    return Json(new { success = true, files = new List<dynamic>() });
                }

                DateTime? start = null;
                DateTime? end = null;

                if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var parsedStart))
                {
                    start = parsedStart;
                }

                if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
                {
                    end = parsedEnd.AddDays(1).AddSeconds(-1);
                }

                var files = Directory.GetFiles(recordDir, "*.csv")
                    .Select(filePath => new FileInfo(filePath))
                    .Where(f =>
                    {
                        if (start.HasValue && f.CreationTime < start.Value) return false;
                        if (end.HasValue && f.CreationTime > end.Value) return false;
                        return true;
                    })
                    .Select(f => new
                    {
                        name = f.Name,
                        path = f.FullName,
                        size = f.Length,
                        created = f.CreationTime
                    })
                    .OrderByDescending(f => f.created)
                    .ToList();

                return Json(new { success = true, files = files });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 下载删除记录压缩包
        /// </summary>
        [HttpGet]
        public IActionResult DownloadDeleteRecords(string startDate = null, string endDate = null)
        {
            try
            {
                string recordDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "record", "AutoDeleteFile");
                if (!Directory.Exists(recordDir))
                {
                    return Json(new { success = false, message = "没有找到记录文件" });
                }

                DateTime? start = null;
                DateTime? end = null;

                if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var parsedStart))
                {
                    start = parsedStart;
                }

                if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
                {
                    end = parsedEnd.AddDays(1).AddSeconds(-1);
                }

                var files = Directory.GetFiles(recordDir, "*.csv")
                    .Select(filePath => new FileInfo(filePath))
                    .Where(f =>
                    {
                        if (start.HasValue && f.CreationTime < start.Value) return false;
                        if (end.HasValue && f.CreationTime > end.Value) return false;
                        return true;
                    })
                    .Select(f => f.FullName)
                    .ToList();

                if (files.Count == 0)
                {
                    return Json(new { success = false, message = "没有找到要下载的文件" });
                }

                var zipFileName = $"DeleteRecords_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                var zipPath = Path.Combine(Path.GetTempPath(), zipFileName);

                using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (var filePath in files)
                    {
                        var entryName = Path.GetFileName(filePath);
                        zipArchive.CreateEntryFromFile(filePath, entryName);
                    }
                }

                var fileBytes = System.IO.File.ReadAllBytes(zipPath);

                try
                {
                    System.IO.File.Delete(zipPath);
                }
                catch { }

                return base.File(fileBytes, "application/zip", zipFileName);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 保存磁盘配置请求模型
    /// </summary>
    public class SaveDiskConfigRequest
    {
        public string DriveLetter { get; set; } = string.Empty;
        public List<string> DeleteDirectories { get; set; } = new();
        public double StartDeleteSizeGB { get; set; }
        public double StopDeleteSizeGB { get; set; }
        public int StartDeleteFileDays { get; set; }
        public string LogicMode { get; set; } = "OR";
    }

    /// <summary>
    /// 删除磁盘配置请求模型
    /// </summary>
    public class DeleteDiskConfigRequest
    {
        public string DriveLetter { get; set; } = string.Empty;
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
