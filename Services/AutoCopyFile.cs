using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSysWatch
{    /// <summary>
    /// 自动拷贝文件配置
    /// </summary>
    public class AutoCopyConfig
    {
        /// <summary>
        /// 源目录
        /// </summary>
        public string SourceDirectory { get; set; } = "";
        
        /// <summary>
        /// 目标目录
        /// </summary>
        public string TargetDirectory { get; set; } = "";
        
        /// <summary>
        /// 已拷贝文件移动目录（可选，如果设置则将已拷贝的文件移动到此目录）
        /// </summary>
        public string? MovedDirectory { get; set; }
    }

    public class AutoCopyFile
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoCopyFile.ini");
        private static List<AutoCopyConfig> _configs = new List<AutoCopyConfig>();

        /// <summary>
        /// 开始自动拷贝
        /// </summary>
        public static void Start()
        {
            LogHelper.Logger.Information("自动拷贝文件开始");
            Task.Run(Run);
        }

        /// <summary>
        /// 执行拷贝任务
        /// </summary>
        private static void Run()
        {
            while (true)
            {
                try
                {
                    // 读取配置文件
                    ReadIniFile();

                    // 执行每个配置的拷贝任务
                    foreach (var config in _configs)
                    {
                        ExecuteCopyTask(config);
                    }

                    LogHelper.Logger.Information("自动拷贝文件任务完成");
                }
                catch (Exception ex)
                {
                    LogHelper.Logger.Error($"自动拷贝文件异常：{ex.Message}", ex);
                }
                finally
                {
                    Thread.Sleep(1000*10);
                }
            }
        }

        /// <summary>
        /// 读取INI配置文件
        /// </summary>
        private static void ReadIniFile()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    CreateDefaultIniFile();
                    return;
                }

                _configs.Clear();
                var lines = File.ReadAllLines(ConfigFilePath, Encoding.UTF8);
                AutoCopyConfig currentConfig = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        // 新的拷贝配置节
                        if (currentConfig != null)
                        {
                            _configs.Add(currentConfig);
                        }
                        currentConfig = new AutoCopyConfig();
                    }
                    else if (currentConfig != null && trimmedLine.Contains("="))
                    {
                        var parts = trimmedLine.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim().ToLower();
                            var value = parts[1].Trim();

                            switch (key)
                            {
                                case "sourcedirectory":
                                    currentConfig.SourceDirectory = value;
                                    break;
                                case "targetdirectory":
                                    currentConfig.TargetDirectory = value;
                                    break;
                                case "moveddirectory":
                                    currentConfig.MovedDirectory = value;
                                    break;
                            }
                        }
                    }
                }

                // 添加最后一个配置
                if (currentConfig != null)
                {
                    _configs.Add(currentConfig);
                }

                LogHelper.Logger.Information($"读取拷贝配置文件成功，共{_configs.Count}个配置");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"读取拷贝配置文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        private static void CreateDefaultIniFile()
        {
            try
            {
                var defaultContent = @"# 自动拷贝文件配置
# 支持配置多个拷贝任务，每个任务使用[CopyTask]开始

[CopyTask]
# 源目录 - 要拷贝的图片文件所在目录
SourceDirectory=D:\Photos
# 目标目录 - 拷贝到的目标目录
TargetDirectory=E:\Backup\Photos
# 已拷贝文件移动目录 - 拷贝完成后将源文件移动到此目录（可选）
MovedDirectory=D:\Photos_Processed

# 可以添加更多拷贝任务
#[CopyTask]
#SourceDirectory=C:\Images
#TargetDirectory=D:\Backup\Images
#MovedDirectory=C:\Images_Processed
";

                File.WriteAllText(ConfigFilePath, defaultContent, Encoding.UTF8);
                LogHelper.Logger.Information($"创建默认拷贝配置文件：{ConfigFilePath}");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"创建默认拷贝配置文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行拷贝任务
        /// </summary>
        /// <param name="config">拷贝配置</param>
        private static void ExecuteCopyTask(AutoCopyConfig config)
        {
            try
            {
                if (string.IsNullOrEmpty(config.SourceDirectory) || string.IsNullOrEmpty(config.TargetDirectory))
                {
                    LogHelper.Logger.Warning("拷贝配置不完整，跳过该任务");
                    return;
                }

                if (!Directory.Exists(config.SourceDirectory))
                {
                    LogHelper.Logger.Warning($"源目录不存在：{config.SourceDirectory}");
                    return;
                }

                // 确保目标目录存在
                if (!Directory.Exists(config.TargetDirectory))
                {
                    Directory.CreateDirectory(config.TargetDirectory);
                    LogHelper.Logger.Information($"创建目标目录：{config.TargetDirectory}");
                }

                // 如果配置了MovedDirectory，确保该目录存在
                if (!string.IsNullOrEmpty(config.MovedDirectory) && !Directory.Exists(config.MovedDirectory))
                {
                    Directory.CreateDirectory(config.MovedDirectory);
                    LogHelper.Logger.Information($"创建已处理文件目录：{config.MovedDirectory}");
                }

                // 创建当日记录文件路径
                var dailyRecordPath = GetDailyRecordPath(config.SourceDirectory);
                InitializeDailyRecord(dailyRecordPath);

                // 获取所有图片文件
                var imageFiles = GetImageFiles(config.SourceDirectory);

                int copiedCount = 0;
                foreach (var imageFile in imageFiles)
                {
                    if (CopyAndMoveImageFile(imageFile, config.SourceDirectory, config.TargetDirectory, config.MovedDirectory, dailyRecordPath))
                    {
                        copiedCount++;
                    }
                }

                LogHelper.Logger.Information($"拷贝任务完成，从 {config.SourceDirectory} 到 {config.TargetDirectory}，共拷贝 {copiedCount} 个文件");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"执行拷贝任务异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取目录下所有图片文件
        /// </summary>
        /// <param name="directory">目录路径</param>
        /// <returns>图片文件路径列表</returns>
        private static List<string> GetImageFiles(string directory)
        {
            var imageFiles = new List<string>();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif", ".webp", ".ico" };
            try
            {
                var allFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                
                foreach (var file in allFiles)
                {
                    var fileName = Path.GetFileName(file);
                    
                    // 排除所有拷贝记录文件（包括历史记录和当日记录）
                    if (fileName.Equals("拷贝记录.txt", StringComparison.OrdinalIgnoreCase) ||
                        fileName.StartsWith("拷贝记录_", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var extension = Path.GetExtension(file).ToLower();
                    if (imageExtensions.Contains(extension))
                    {
                        imageFiles.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"获取图片文件异常：{ex.Message}", ex);
            }

            return imageFiles;
        }

        /// <summary>
        /// 获取当日记录文件路径
        /// </summary>
        /// <param name="sourceDirectory">源目录</param>
        /// <returns>当日记录文件路径</returns>
        private static string GetDailyRecordPath(string sourceDirectory)
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(sourceDirectory, $"拷贝记录_{today}.txt");
        }

        /// <summary>
        /// 初始化当日记录文件
        /// </summary>
        /// <param name="recordPath">记录文件路径</param>
        private static void InitializeDailyRecord(string recordPath)
        {
            try
            {
                if (!File.Exists(recordPath))
                {
                    var header = "时间,操作类型,源文件路径,目标文件路径,文件大小(字节),状态" + Environment.NewLine;
                    File.WriteAllText(recordPath, header, Encoding.UTF8);
                    LogHelper.Logger.Information($"创建当日记录文件：{recordPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"初始化当日记录文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 写入操作记录
        /// </summary>
        /// <param name="recordPath">记录文件路径</param>
        /// <param name="operationType">操作类型</param>
        /// <param name="sourceFile">源文件路径</param>
        /// <param name="targetFile">目标文件路径</param>
        /// <param name="fileSize">文件大小</param>
        /// <param name="status">操作状态</param>
        private static void WriteOperationRecord(string recordPath, string operationType, string sourceFile, string targetFile, long fileSize, string status)
        {
            try
            {
                var record = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{operationType},\"{sourceFile}\",\"{targetFile}\",{fileSize},{status}";
                File.AppendAllText(recordPath, record + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"写入操作记录异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 拷贝图片文件并移动源文件
        /// </summary>
        /// <param name="sourceFile">源文件路径</param>
        /// <param name="sourceRoot">源根目录</param>
        /// <param name="targetRoot">目标根目录</param>
        /// <param name="movedRoot">已处理文件移动目录（可选）</param>
        /// <param name="recordPath">记录文件路径</param>
        /// <returns>是否处理成功</returns>
        private static bool CopyAndMoveImageFile(string sourceFile, string sourceRoot, string targetRoot, string? movedRoot, string recordPath)
        {
            try
            {
                // 计算相对路径，保持目录结构
                var relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
                var targetFile = Path.Combine(targetRoot, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);
                var fileInfo = new FileInfo(sourceFile);

                // 确保目标目录存在
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // 如果目标文件已存在，比较文件大小和修改时间
                if (File.Exists(targetFile))
                {
                    var targetInfo = new FileInfo(targetFile);

                    if (fileInfo.Length == targetInfo.Length &&
                        fileInfo.LastWriteTime <= targetInfo.LastWriteTime)
                    {
                        // 文件相同，记录跳过操作
                        WriteOperationRecord(recordPath, "跳过拷贝", sourceFile, targetFile, fileInfo.Length, "文件已存在且相同");
                        
                        // 直接移动源文件到已处理目录（如果配置了的话）
                        MoveSourceFileToProcessed(sourceFile, sourceRoot, movedRoot, recordPath, fileInfo.Length);
                        LogHelper.Logger.Information($"文件已存在且相同，移动源文件：{relativePath}");
                        return true;
                    }
                }

                // 执行拷贝
                File.Copy(sourceFile, targetFile, true);
                LogHelper.Logger.Information($"拷贝文件：{relativePath}");
                
                // 记录拷贝操作
                WriteOperationRecord(recordPath, "拷贝", sourceFile, targetFile, fileInfo.Length, "成功");

                // 拷贝成功后，移动源文件到已处理目录
                MoveSourceFileToProcessed(sourceFile, sourceRoot, movedRoot, recordPath, fileInfo.Length);

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"处理文件异常 {sourceFile}：{ex.Message}", ex);
                
                // 记录失败操作
                try
                {
                    var fileInfo = new FileInfo(sourceFile);
                    WriteOperationRecord(recordPath, "拷贝", sourceFile, "N/A", fileInfo.Length, $"失败：{ex.Message}");
                }
                catch { }
                
                return false;
            }
        }

        /// <summary>
        /// 将源文件移动到已处理目录
        /// </summary>
        /// <param name="sourceFile">源文件路径</param>
        /// <param name="sourceRoot">源根目录</param>
        /// <param name="movedRoot">已处理文件移动目录</param>
        /// <param name="recordPath">记录文件路径</param>
        /// <param name="fileSize">文件大小</param>
        private static void MoveSourceFileToProcessed(string sourceFile, string sourceRoot, string? movedRoot, string recordPath, long fileSize)
        {
            try
            {
                var relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
                
                // 如果没有配置移动目录，则删除源文件
                if (string.IsNullOrEmpty(movedRoot))
                {
                    File.Delete(sourceFile);
                    WriteOperationRecord(recordPath, "删除", sourceFile, "N/A", fileSize, "成功");
                    LogHelper.Logger.Information($"删除已处理的源文件：{relativePath}");
                    return;
                }

                // 计算在已处理目录中的路径
                var movedFile = Path.Combine(movedRoot, relativePath);
                var movedDir = Path.GetDirectoryName(movedFile);

                // 确保移动目标目录存在
                if (!string.IsNullOrEmpty(movedDir) && !Directory.Exists(movedDir))
                {
                    Directory.CreateDirectory(movedDir);
                }

                // 如果目标文件已存在，删除它（或者可以重命名）
                if (File.Exists(movedFile))
                {
                    File.Delete(movedFile);
                }

                // 移动文件
                File.Move(sourceFile, movedFile);
                WriteOperationRecord(recordPath, "移动", sourceFile, movedFile, fileSize, "成功");
                LogHelper.Logger.Information($"移动已处理文件：{relativePath} -> 已处理目录");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"移动文件到已处理目录异常：{ex.Message}", ex);
                WriteOperationRecord(recordPath, "移动", sourceFile, movedRoot ?? "N/A", fileSize, $"失败：{ex.Message}");
                
                // 如果移动失败，至少删除源文件
                try
                {
                    File.Delete(sourceFile);
                    WriteOperationRecord(recordPath, "删除", sourceFile, "N/A", fileSize, "移动失败后删除成功");
                    LogHelper.Logger.Information($"移动失败，删除源文件：{Path.GetRelativePath(sourceRoot, sourceFile)}");
                }
                catch (Exception deleteEx)
                {
                    LogHelper.Logger.Error($"删除源文件也失败：{deleteEx.Message}", deleteEx);
                    WriteOperationRecord(recordPath, "删除", sourceFile, "N/A", fileSize, $"删除失败：{deleteEx.Message}");
                }
            }
        }

        /// <summary>
        /// 停止自动拷贝
        /// </summary>
        public static void Stop()
        {
            LogHelper.Logger.Information("自动拷贝文件停止");
        }
    }
}