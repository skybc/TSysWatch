using Microsoft.AspNetCore.Mvc;
using TSysWatch.Entity;
using System.Text.Json;

namespace TSysWatch.Controllers
{
    public class FileManagerController : Controller
    {
        private readonly ILogger<FileManagerController> _logger;
        private readonly List<string> _allowedUploadExtensions = new() { ".txt", ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".zip", ".rar", ".doc", ".docx", ".xls", ".xlsx" };
        private readonly List<string> _systemProtectedPaths = new() { "C:\\Windows", "C:\\Program Files", "C:\\Program Files (x86)", "C:\\System Volume Information", "C:\\$Recycle.Bin" };

        public FileManagerController(ILogger<FileManagerController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 显示文件管理页面
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public IActionResult Index(string path = "")
        {
            try
            {
                // 默认路径为第一个可用的非C盘驱动器
                if (string.IsNullOrEmpty(path))
                {
                    var availableDrives = GetAvailableDrives();
                    if (availableDrives.Any())
                    {
                        path = availableDrives.First().Name;
                    }
                    else
                    {
                        // 如果没有可用驱动器，使用当前目录（但检查是否在C盘）
                        path = Directory.GetCurrentDirectory();
                        var drive = Path.GetPathRoot(path)?.ToUpper();
                        if (drive == "C:\\")
                        {
                            TempData["Error"] = "没有可用的非系统驱动器。";
                            return View(new FileManagerViewModel());
                        }
                    }
                }

                // 路径安全检查
                if (!IsPathSafe(path))
                {
                    TempData["Error"] = "访问被拒绝：不允许访问系统盘（C盘）或系统敏感目录。";
                    var availableDrives = GetAvailableDrives();
                    if (availableDrives.Any())
                    {
                        path = availableDrives.First().Name;
                    }
                    else
                    {
                        return View(new FileManagerViewModel());
                    }
                }

                // 确保路径存在
                if (!Directory.Exists(path))
                {
                    TempData["Error"] = "指定路径不存在。";
                    var availableDrives = GetAvailableDrives();
                    if (availableDrives.Any())
                    {
                        path = availableDrives.First().Name;
                    }
                    else
                    {
                        return View(new FileManagerViewModel());
                    }
                }

                var model = new FileManagerViewModel
                {
                    CurrentPath = path,
                    Files = GetFilesAndDirectories(path),
                    Breadcrumbs = GenerateBreadcrumbs(path),
                    AvailableDrives = GetAvailableDrives()
                };

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "访问被拒绝：没有权限访问该目录。";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "访问目录时发生错误: {Path}", path);
                TempData["Error"] = "访问目录时发生错误。";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public IActionResult Download(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    return NotFound("文件不存在。");
                }

                // 路径安全检查
                if (!IsPathSafe(filePath))
                {
                    return BadRequest("访问被拒绝：不允许下载系统敏感目录中的文件。");
                }

                var fileName = Path.GetFileName(filePath);
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载文件时发生错误: {FilePath}", filePath);
                return BadRequest("下载文件时发生错误。");
            }
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="files">上传的文件</param>
        /// <param name="path">上传路径</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Upload(List<IFormFile> files, string path)
        {
            try
            {
                if (!Directory.Exists(path) || !IsPathSafe(path))
                {
                    TempData["Error"] = "上传路径无效。";
                    return RedirectToAction("Index", new { path });
                }

                if (files == null || files.Count == 0)
                {
                    TempData["Error"] = "请选择要上传的文件。";
                    return RedirectToAction("Index", new { path });
                }

                int successCount = 0;
                int failCount = 0;
                var errors = new List<string>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        
                        // 检查文件类型
                        if (!_allowedUploadExtensions.Contains(extension))
                        {
                            errors.Add($"文件 {file.FileName} 类型不被允许。");
                            failCount++;
                            continue;
                        }

                        // 检查文件大小（限制为50MB）
                        if (file.Length > 50 * 1024 * 1024)
                        {
                            errors.Add($"文件 {file.FileName} 超过大小限制（50MB）。");
                            failCount++;
                            continue;
                        }

                        var filePath = Path.Combine(path, file.FileName);
                        
                        // 如果文件已存在，添加时间戳
                        if (System.IO.File.Exists(filePath))
                        {
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
                            var ext = Path.GetExtension(file.FileName);
                            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            filePath = Path.Combine(path, $"{nameWithoutExt}_{timestamp}{ext}");
                        }

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        successCount++;
                    }
                }

                if (successCount > 0)
                {
                    TempData["Success"] = $"成功上传 {successCount} 个文件。";
                }

                if (failCount > 0)
                {
                    TempData["Error"] = $"上传失败 {failCount} 个文件：" + string.Join("; ", errors);
                }

                return RedirectToAction("Index", new { path });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传文件时发生错误");
                TempData["Error"] = "上传文件时发生错误。";
                return RedirectToAction("Index", new { path });
            }
        }

        /// <summary>
        /// 删除文件或文件夹
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Delete(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return Json(new { success = false, message = "路径不能为空。" });
                }

                // 路径安全检查
                if (!IsPathSafe(path))
                {
                    return Json(new { success = false, message = "访问被拒绝：不允许删除系统敏感目录。" });
                }

                // 检查是否在系统盘
                var drive = Path.GetPathRoot(path)?.ToUpper();
                if (drive == "C:\\")
                {
                    return Json(new { success = false, message = "不允许删除系统盘（C盘）中的文件或文件夹。" });
                }

                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    return Json(new { success = true, message = "文件删除成功。" });
                }
                else if (Directory.Exists(path))
                {
                    // 检查文件夹是否为空
                    if (Directory.GetFileSystemEntries(path).Length > 0)
                    {
                        return Json(new { success = false, message = "只能删除空文件夹。" });
                    }
                    
                    Directory.Delete(path);
                    return Json(new { success = true, message = "文件夹删除成功。" });
                }
                else
                {
                    return Json(new { success = false, message = "文件或文件夹不存在。" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除文件时发生错误: {Path}", path);
                return Json(new { success = false, message = "删除时发生错误。" });
            }
        }

        /// <summary>
        /// 检查路径是否安全
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        private bool IsPathSafe(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                // 获取绝对路径
                var fullPath = Path.GetFullPath(path);
                
                // 检查是否包含路径穿越
                if (fullPath.Contains(".."))
                    return false;

                // 检查是否是C盘
                var drive = Path.GetPathRoot(fullPath)?.ToUpper();
                if (drive == "C:\\")
                    return false;

                // 检查是否访问系统保护路径（仅限C盘的系统目录）
                return !_systemProtectedPaths.Any(protectedPath => 
                    fullPath.StartsWith(protectedPath, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取目录下的文件和文件夹
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        private List<FileViewModel> GetFilesAndDirectories(string path)
        {
            var result = new List<FileViewModel>();

            try
            {
                // 添加文件夹
                var directories = Directory.GetDirectories(path);
                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    result.Add(new FileViewModel
                    {
                        Name = dirInfo.Name,
                        Path = dir,
                        IsDirectory = true,
                        Size = 0
                    });
                }

                // 添加文件
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    result.Add(new FileViewModel
                    {
                        Name = fileInfo.Name,
                        Path = file,
                        IsDirectory = false,
                        Size = fileInfo.Length
                    });
                }

                // 按类型和名称排序（文件夹优先）
                return result.OrderBy(f => !f.IsDirectory).ThenBy(f => f.Name).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取目录内容时发生错误: {Path}", path);
                return result;
            }
        }

        /// <summary>
        /// 生成面包屑导航
        /// </summary>
        /// <param name="path">当前路径</param>
        /// <returns></returns>
        private List<BreadcrumbItem> GenerateBreadcrumbs(string path)
        {
            var breadcrumbs = new List<BreadcrumbItem>();
            
            try
            {
                var parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                var currentPath = "";
                
                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(currentPath))
                    {
                        currentPath = part + Path.DirectorySeparatorChar;
                    }
                    else
                    {
                        currentPath = Path.Combine(currentPath, part);
                    }

                    breadcrumbs.Add(new BreadcrumbItem
                    {
                        Name = part,
                        Path = currentPath
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成面包屑导航时发生错误: {Path}", path);
            }

            return breadcrumbs;
        }

        /// <summary>
        /// 获取可用驱动器列表（排除C盘）
        /// </summary>
        /// <returns></returns>
        private List<DriveViewModel> GetAvailableDrives()
        {
            var drives = new List<DriveViewModel>();
            
            try
            {
                var allDrives = DriveInfo.GetDrives();
                
                foreach (var drive in allDrives)
                {
                    // 排除C盘和不可用的驱动器
                    if (drive.Name.ToUpper() == "C:\\" || !drive.IsReady)
                        continue;
                        
                    var driveViewModel = new DriveViewModel
                    {
                        Name = drive.Name,
                        Label = drive.IsReady ? (string.IsNullOrEmpty(drive.VolumeLabel) ? "本地磁盘" : drive.VolumeLabel) : "未准备好",
                        DriveType = GetDriveTypeDescription(drive.DriveType),
                        IsReady = drive.IsReady
                    };
                    
                    if (drive.IsReady)
                    {
                        try
                        {
                            driveViewModel.TotalSize = drive.TotalSize;
                            driveViewModel.AvailableSpace = drive.AvailableFreeSpace;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "获取驱动器 {DriveName} 空间信息时发生错误", drive.Name);
                            driveViewModel.IsReady = false;
                        }
                    }
                    
                    drives.Add(driveViewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取驱动器列表时发生错误");
            }

            return drives.OrderBy(d => d.Name).ToList();
        }

        /// <summary>
        /// 获取驱动器类型的中文描述
        /// </summary>
        /// <param name="driveType">驱动器类型</param>
        /// <returns></returns>
        private static string GetDriveTypeDescription(DriveType driveType)
        {
            return driveType switch
            {
                DriveType.Fixed => "本地磁盘",
                DriveType.Removable => "可移动磁盘",
                DriveType.Network => "网络驱动器",
                DriveType.CDRom => "光驱",
                DriveType.Ram => "RAM磁盘",
                _ => "未知"
            };
        }
    }
}
