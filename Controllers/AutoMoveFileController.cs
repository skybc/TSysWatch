using Microsoft.AspNetCore.Mvc;
using System;
using TSysWatch;

namespace TSysWatch.Controllers
{
    /// <summary>
    /// 自动移动文件管理控制器
    /// </summary>
    public class AutoMoveFileController : Controller
    {
        /// <summary>
        /// 自动移动文件管理页面
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var configs = AutoMoveFileManager.GetCurrentConfigs();
            var drives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable || d.DriveType == DriveType.Network)
                .Select(d => new AutoMoveFilePageViewModel.DriveOption
                {
                    Name = d.Name.TrimEnd('\\'),
                    Label = d.Name.TrimEnd('\\') + (string.IsNullOrEmpty(d.VolumeLabel) ? "" : " (" + d.VolumeLabel + ")")
                })
                .ToList();

            var configDisplays = configs.Select(config => new AutoMoveConfigDisplay
            {
                SourceDirectory = config.SourceDirectory,
                TargetDrive = config.TargetDrive,
                MoveTimeLimitMinutes = config.MoveTimeLimitMinutes,
                SourceDirExists = AutoMoveFileManager.CheckDirectoryExists(config.SourceDirectory),
                TargetDriveExists = DriveInfo.GetDrives().Any(d => d.Name.TrimEnd('\\').Equals(config.TargetDrive, StringComparison.OrdinalIgnoreCase)),
                FileCount = AutoMoveFileManager.GetFileCount(config.SourceDirectory),
                DirectorySize = AutoMoveFileManager.FormatBytes(AutoMoveFileManager.GetDirectorySize(config.SourceDirectory))
            }).ToList();

            var viewModel = new AutoMoveFilePageViewModel
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
                var configs = AutoMoveFileManager.GetCurrentConfigs();

                var configDisplays = configs.Select(config => new AutoMoveConfigDisplay
                {
                    SourceDirectory = config.SourceDirectory,
                    TargetDrive = config.TargetDrive,
                    MoveTimeLimitMinutes = config.MoveTimeLimitMinutes,
                    SourceDirExists = AutoMoveFileManager.CheckDirectoryExists(config.SourceDirectory),
                    TargetDriveExists = DriveInfo.GetDrives().Any(d => d.Name.TrimEnd('\\').Equals(config.TargetDrive, StringComparison.OrdinalIgnoreCase)),
                    FileCount = AutoMoveFileManager.GetFileCount(config.SourceDirectory),
                    DirectorySize = AutoMoveFileManager.FormatBytes(AutoMoveFileManager.GetDirectorySize(config.SourceDirectory))
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
                var configs = AutoMoveFileManager.GetCurrentConfigs();

                var result = configs.Select(config => new
                {
                    SourceDirectory = config.SourceDirectory,
                    TargetDrive = config.TargetDrive,
                    MoveTimeLimitMinutes = config.MoveTimeLimitMinutes,
                    SourceDirExists = AutoMoveFileManager.CheckDirectoryExists(config.SourceDirectory),
                    TargetDriveExists = DriveInfo.GetDrives().Any(d => d.Name.TrimEnd('\\').Equals(config.TargetDrive, StringComparison.OrdinalIgnoreCase)),
                    FileCount = AutoMoveFileManager.GetFileCount(config.SourceDirectory),
                    DirectorySize = AutoMoveFileManager.FormatBytes(AutoMoveFileManager.GetDirectorySize(config.SourceDirectory))
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
        /// 添加或更新移动配置的API
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult SaveConfig([FromBody] SaveConfigRequest request)
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

                AutoMoveFileManager.AddOrUpdateConfig(request.SourceDirectory, request.TargetDrive, request.MoveTimeLimitMinutes);

                return Json(new { success = true, message = "配置已保存" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除移动配置的API
        /// </summary>
        /// <param name="sourceDirectory">源目录</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult DeleteConfig([FromBody] DeleteConfigRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SourceDirectory))
                {
                    return Json(new { success = false, message = "源目录不能为空" });
                }

                AutoMoveFileManager.RemoveConfig(request.SourceDirectory);

                return Json(new { success = true, message = "配置已删除" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 检查目录的API
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult CheckDirectory([FromBody] CheckDirectoryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.DirectoryPath))
                {
                    return Json(new { success = false, message = "目录路径不能为空" });
                }

                bool exists = Directory.Exists(request.DirectoryPath);
                int fileCount = exists ? AutoMoveFileManager.GetFileCount(request.DirectoryPath) : 0;
                string dirSize = exists ? AutoMoveFileManager.FormatBytes(AutoMoveFileManager.GetDirectorySize(request.DirectoryPath)) : "0 Bytes";

                return Json(new
                {
                    success = true,
                    exists = exists,
                    fileCount = fileCount,
                    directorySize = dirSize
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 保存配置请求模型
    /// </summary>
    public class SaveConfigRequest
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
        /// 移动时间限制（分钟）
        /// </summary>
        public int MoveTimeLimitMinutes { get; set; } = 0;
    }

    /// <summary>
    /// 删除配置请求模型
    /// </summary>
    public class DeleteConfigRequest
    {
        /// <summary>
        /// 源目录
        /// </summary>
        public string SourceDirectory { get; set; } = "";
    }

    /// <summary>
    /// 检查目录请求模型
    /// </summary>
    public class CheckDirectoryRequest
    {
        /// <summary>
        /// 目录路径
        /// </summary>
        public string DirectoryPath { get; set; } = "";
    }
}
