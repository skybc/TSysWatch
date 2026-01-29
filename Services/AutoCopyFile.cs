using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        /// 目标磁盘（如 D:、E: 等）
        /// 文件会保持相对路径结构拷贝到目标磁盘
        /// </summary>
        public string TargetDrive { get; set; } = "";
        
        /// <summary>
        /// 已拷贝文件移动目录（可选，如果设置则将已拷贝的文件移动到此目录）
        /// </summary>
        public string? MovedDirectory { get; set; }
    }

    public class AutoCopyFile
    {
        private static readonly string ConfigDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirPath, "AutoCopyFile.json");
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
            List<AutoCopyConfig> configCache = null;
            int configCheckInterval = 0;
            const int configCheckFrequency = 2; // 每2次循环检查一次配置（约20秒）
            
            while (true)
            {
                try
                {
                    // 定期重新读取配置
                    if (configCheckInterval++ >= configCheckFrequency)
                    {
                        ReadJsonFile();
                        configCache = _configs;
                        configCheckInterval = 0;
                    }

                    // 执行每个配置的拷贝任务
                    if (configCache != null && configCache.Count > 0)
                    {
                        foreach (var config in configCache)
                        {
                            ExecuteCopyTask(config);
                        }
                        LogHelper.Logger.Information("自动拷贝文件任务完成");
                    }
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
        /// 读取JSON配置文件
        /// </summary>
        private static void ReadJsonFile()
        {
            try
            {
                EnsureConfigDirectory();
                if (!File.Exists(ConfigFilePath))
                {
                    CreateDefaultJsonFile();
                    return;
                }

                _configs.Clear();
                var json = File.ReadAllText(ConfigFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _configs = JsonSerializer.Deserialize<List<AutoCopyConfig>>(json, options) ?? new List<AutoCopyConfig>();

                LogHelper.Logger.Information($"读取拷贝配置文件成功，共{_configs.Count}个配置");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"读取拷贝配置文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>        /// 确保配置目录存在
        /// </summary>
        private static void EnsureConfigDirectory()
        {
            try
            {
                if (!Directory.Exists(ConfigDirPath))
                {
                    Directory.CreateDirectory(ConfigDirPath);
                    LogHelper.Logger.Information($"创建配置目录：{ConfigDirPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"创建配置目录异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        private static void CreateDefaultJsonFile()
        {
            try
            {
                EnsureConfigDirectory();
                var defaultConfigs = new List<AutoCopyConfig>
                {
                    new AutoCopyConfig
                    {
                        SourceDirectory = "D:\\Photos",
                        TargetDrive = "E:",
                        MovedDirectory = "D:\\Photos_Processed"
                    }
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(defaultConfigs, options);
                File.WriteAllText(ConfigFilePath, json, Encoding.UTF8);
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
                if (string.IsNullOrEmpty(config.SourceDirectory) || string.IsNullOrEmpty(config.TargetDrive))
                {
                    LogHelper.Logger.Warning("拷贝配置不完整，跳过该任务");
                    return;
                }

                if (!Directory.Exists(config.SourceDirectory))
                {
                    LogHelper.Logger.Warning($"源目录不存在：{config.SourceDirectory}");
                    return;
                }

                // 验证源目录和目标磁盘不在同一磁盘
                string sourceDrive = Path.GetPathRoot(config.SourceDirectory)?.TrimEnd('\\') ?? "";
                if (sourceDrive.Equals(config.TargetDrive, StringComparison.OrdinalIgnoreCase))
                {
                    LogHelper.Logger.Warning($"源目录磁盘 {sourceDrive} 与目标磁盘 {config.TargetDrive} 相同，跳过该任务");
                    return;
                }

                // 验证目标磁盘存在
                if (!DriveInfo.GetDrives().Any(d => d.Name.TrimEnd('\\').Equals(config.TargetDrive, StringComparison.OrdinalIgnoreCase)))
                {
                    LogHelper.Logger.Warning($"目标磁盘不存在：{config.TargetDrive}");
                    return;
                }

                // 创建记录文件路径
                var recordPath = GetCopyRecordPath();
                InitializeCopyRecord(recordPath);

                // 获取所有图片文件
                var imageFiles = GetImageFiles(config.SourceDirectory);
                
                // 如果没有文件需要拷贝，释放内存并返回
                if (imageFiles == null || imageFiles.Count == 0)
                {
                    LogHelper.Logger.Information($"源目录 {config.SourceDirectory} 没有需要拷贝的文件");
                    return;
                }

                int copiedCount = 0;
                long totalCopiedSize = 0;

                foreach (var imageFile in imageFiles)
                {
                    try
                    {
                        var result = CopyImageFileWithStructure(imageFile, config.SourceDirectory, config.TargetDrive, recordPath);
                        if (result.success)
                        {
                            copiedCount++;
                            totalCopiedSize += result.fileSize;

                            // 如果配置了MovedDirectory，移动源文件
                            if (!string.IsNullOrEmpty(config.MovedDirectory))
                            {
                                try
                                {
                                    if (!Directory.Exists(config.MovedDirectory))
                                    {
                                        Directory.CreateDirectory(config.MovedDirectory);
                                    }

                                    string movedFilePath = Path.Combine(config.MovedDirectory, Path.GetFileName(imageFile));
                                    File.Move(imageFile, movedFilePath, true);
                                    LogHelper.Logger.Information($"源文件已移动：{imageFile} -> {movedFilePath}");
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.Logger.Error($"移动源文件失败：{imageFile}，错误：{ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Logger.Error($"处理文件异常：{imageFile}，错误：{ex.Message}");
                    }
                }

                LogHelper.Logger.Information($"拷贝任务完成，从 {config.SourceDirectory} 到 {config.TargetDrive}，共拷贝 {copiedCount} 个文件，总大小 {totalCopiedSize / (1024.0 * 1024.0):F2}MB");
                
                // 释放文件列表内存
                imageFiles.Clear();
                imageFiles = null;
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
        /// 获取拷贝记录文件路径（CSV格式）
        /// </summary>
        /// <returns>记录文件路径</returns>
        private static string GetCopyRecordPath()
        {
            string recordDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "record", "AutoCopyFile");
            string today = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(recordDir, $"{today}.csv");
        }

        /// <summary>
        /// 初始化拷贝记录文件
        /// </summary>
        /// <param name="recordPath">记录文件路径</param>
        private static void InitializeCopyRecord(string recordPath)
        {
            try
            {
                string recordDir = Path.GetDirectoryName(recordPath) ?? "";
                if (!Directory.Exists(recordDir))
                {
                    Directory.CreateDirectory(recordDir);
                }

                if (!File.Exists(recordPath))
                {
                    var header = "时间,原文件,目标文件" + Environment.NewLine;
                    File.WriteAllText(recordPath, header, Encoding.UTF8);
                    LogHelper.Logger.Information($"创建拷贝记录文件：{recordPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"初始化拷贝记录文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 拷贝图片文件并保持目录结构
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceRootDirectory">源根目录</param>
        /// <param name="targetDrive">目标磁盘</param>
        /// <param name="recordPath">记录文件路径</param>
        /// <returns>拷贝结果</returns>
        private static (bool success, long fileSize) CopyImageFileWithStructure(string sourceFilePath, string sourceRootDirectory, string targetDrive, string recordPath)
        {
            try
            {
                // 获取源目录的完整路径名（不含盘符）
                // 例如：D:\Apache24\icons -> \Apache24\icons
                string sourcePath = Path.GetFullPath(sourceRootDirectory);
                string sourcePathWithoutDrive = sourcePath.Substring(2); // 移除盘符和冒号 (D:)
                
                // 计算相对路径（源文件相对于源根目录）
                string relativePath = Path.GetRelativePath(sourceRootDirectory, sourceFilePath);
                
                // 构建目标路径：目标盘符 + 源目录结构 + 相对路径
                string targetFilePath = Path.Combine(targetDrive, sourcePathWithoutDrive.TrimStart('\\'), relativePath);
                string targetDir = Path.GetDirectoryName(targetFilePath) ?? "";

                // 确保目标目录存在
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // 拷贝文件
                var fileInfo = new FileInfo(sourceFilePath);
                File.Copy(sourceFilePath, targetFilePath, true);

                // 记录到CSV
                WriteCopyRecord(recordPath, sourceFilePath, targetFilePath);

                LogHelper.Logger.Information($"文件拷贝成功：{sourceFilePath} -> {targetFilePath}");
                return (true, fileInfo.Length);
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"拷贝文件失败：{sourceFilePath}，错误：{ex.Message}");
                return (false, 0);
            }
        }
        /// <summary>
        /// 写入拷贝记录
        /// </summary>
        private static void WriteCopyRecord(string recordPath, string sourceFile, string targetFile)
        {
            try
            {
                var record = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},\"{sourceFile}\",\"{targetFile}\"";
                File.AppendAllText(recordPath, record + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"写入拷贝记录异常：{ex.Message}", ex);
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