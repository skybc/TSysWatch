using Microsoft.AspNetCore.Mvc;
using System;
using System.IO.Compression;
using TSysWatch;

namespace TSysWatch.Controllers
{
    /// <summary>
    /// 自动拷贝文件管理控制器
    /// </summary>
    public class AutoCopyFileController : Controller
    {
        /// <summary>
        /// 自动拷贝文件管理页面
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var configs = AutoCopyFileManager.GetCurrentConfigs();
            var drives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable || d.DriveType == DriveType.Network)
                .Select(d => new AutoCopyFilePageViewModel.DriveOption
                {
                    Name = d.Name.TrimEnd('\\'),
                    Label = d.Name.TrimEnd('\\') + (string.IsNullOrEmpty(d.VolumeLabel) ? "" : " (" + d.VolumeLabel + ")")
                })
                .ToList();

            var configDisplays = configs.Select(config => new AutoCopyConfigDisplay
            {
                SourceDirectory = config.SourceDirectory,
                TargetDrive = config.TargetDrive,
                MovedDirectory = config.MovedDirectory,
                SourceDirExists = AutoCopyFileManager.CheckDirectoryExists(config.SourceDirectory),
                TargetDriveExists = DriveInfo.GetDrives().Any(d => d.Name.TrimEnd('\\').Equals(config.TargetDrive, StringComparison.OrdinalIgnoreCase)),
                ImageFileCount = AutoCopyFileManager.GetImageFileCount(config.SourceDirectory),
                DirectorySize = AutoCopyFileManager.FormatBytes(AutoCopyFileManager.GetDirectorySize(config.SourceDirectory))
            }).ToList();

            var viewModel = new AutoCopyFilePageViewModel
            {
                AvailableDrives = drives,
                Configs = configDisplays
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
                var configs = AutoCopyFileManager.GetCurrentConfigs();

                var configDisplays = configs.Select(config => new AutoCopyConfigDisplay
                {
                    SourceDirectory = config.SourceDirectory,
                    TargetDrive = config.TargetDrive,
                    MovedDirectory = config.MovedDirectory,
                    SourceDirExists = AutoCopyFileManager.CheckDirectoryExists(config.SourceDirectory),
                    TargetDriveExists = DriveInfo.GetDrives().Any(d => d.Name.TrimEnd('\\').Equals(config.TargetDrive, StringComparison.OrdinalIgnoreCase)),
                    ImageFileCount = AutoCopyFileManager.GetImageFileCount(config.SourceDirectory),
                    DirectorySize = AutoCopyFileManager.FormatBytes(AutoCopyFileManager.GetDirectorySize(config.SourceDirectory))
                }).ToList();

                return PartialView("_ConfigList", configDisplays);
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
                var configs = AutoCopyFileManager.GetCurrentConfigs();

                var result = configs.Select(config => new
                {
                    SourceDirectory = config.SourceDirectory,
                    TargetDrive = config.TargetDrive,
                    MovedDirectory = config.MovedDirectory,
                    SourceDirExists = AutoCopyFileManager.CheckDirectoryExists(config.SourceDirectory),
                    TargetDriveExists = DriveInfo.GetDrives().Any(d => d.Name.TrimEnd('\\').Equals(config.TargetDrive, StringComparison.OrdinalIgnoreCase)),
                    ImageFileCount = AutoCopyFileManager.GetImageFileCount(config.SourceDirectory),
                    DirectorySize = AutoCopyFileManager.FormatBytes(AutoCopyFileManager.GetDirectorySize(config.SourceDirectory))
                }).ToList();

                return Json(new { success = true, configs = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取可用驱动器的API
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetDrives()
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable || d.DriveType == DriveType.Network)
                    .Select(d => new
                    {
                        name = d.Name.TrimEnd('\\'),
                        label = d.Name.TrimEnd('\\') + (string.IsNullOrEmpty(d.VolumeLabel) ? "" : " (" + d.VolumeLabel + ")")
                    })
                    .ToList();

                return Json(new { success = true, drives = drives });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 添加或更新拷贝配置的API
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult SaveConfig([FromBody] SaveCopyConfigRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SourceDirectory) || string.IsNullOrWhiteSpace(request.TargetDrive))
                {
                    return Json(new { success = false, message = "源目录和目标磁盘不能为空" });
                }

                // 检查源目录是否存在
                if (!Directory.Exists(request.SourceDirectory))
                {
                    return Json(new { success = false, message = "源目录不存在" });
                }

                // 检查源目录和目标磁盘不在同一驱动器
                var sourceDrive = Path.GetPathRoot(request.SourceDirectory)?.TrimEnd('\\') ?? "";
                if (sourceDrive.Equals(request.TargetDrive, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "源目录和目标磁盘不能在同一驱动器" });
                }

                // 检查目标磁盘是否存在
                if (!DriveInfo.GetDrives().Any(d => d.Name.TrimEnd('\\').Equals(request.TargetDrive, StringComparison.OrdinalIgnoreCase)))
                {
                    return Json(new { success = false, message = "目标磁盘不存在" });
                }

                // 如果配置了已拷贝目录，也需要检查
                if (!string.IsNullOrWhiteSpace(request.MovedDirectory))
                {
                    if (!Directory.Exists(request.MovedDirectory))
                    {
                        try
                        {
                            Directory.CreateDirectory(request.MovedDirectory);
                        }
                        catch
                        {
                            return Json(new { success = false, message = "已拷贝文件目录不存在，且无法创建" });
                        }
                    }
                }

                AutoCopyFileManager.AddOrUpdateConfig(request.SourceDirectory, request.TargetDrive, request.MovedDirectory);

                return Json(new { success = true, message = "配置已保存" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除拷贝配置的API
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult DeleteConfig([FromBody] DeleteCopyConfigRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SourceDirectory))
                {
                    return Json(new { success = false, message = "源目录不能为空" });
                }

                AutoCopyFileManager.RemoveConfig(request.SourceDirectory);

                return Json(new { success = true, message = "配置已删除" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取拷贝记录CSV文件列表
        /// </summary>
        [HttpGet]
        public IActionResult GetCopyRecordFiles(string startDate = null, string endDate = null)
        {
            try
            {
                string recordDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "record", "AutoCopyFile");
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
        /// 下载拷贝记录压缩包
        /// </summary>
        [HttpGet]
        public IActionResult DownloadCopyRecords(string startDate = null, string endDate = null)
        {
            try
            {
                string recordDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "record", "AutoCopyFile");
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

                var zipFileName = $"CopyRecords_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
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
    /// 保存拷贝配置请求模型
    /// </summary>
    public class SaveCopyConfigRequest
    {
        /// <summary>
        /// 源目录
        /// </summary>
        public string SourceDirectory { get; set; } = "";

        /// <summary>
        /// 目标磁盘
        /// </summary>
        public string TargetDrive { get; set; } = "";

        /// <summary>
        /// 已拷贝文件目录（可选）
        /// </summary>
        public string? MovedDirectory { get; set; }
    }

    /// <summary>
    /// 删除拷贝配置请求模型
    /// </summary>
    public class DeleteCopyConfigRequest
    {
        /// <summary>
        /// 源目录
        /// </summary>
        public string SourceDirectory { get; set; } = "";
    }
}
