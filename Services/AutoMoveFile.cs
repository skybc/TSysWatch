using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dm.util;

namespace TSysWatch
{
    /// <summary>
    /// 自动移动文件配置
    /// </summary>
    public class AutoMoveConfig
    {
        /// <summary>
        /// 源目录
        /// </summary>
        public string SourceDirectory { get; set; } = "";

        /// <summary>
        /// 目标磁盘（如 D:、E: 等）
        /// 文件会保持相对路径结构移动到目标磁盘
        /// </summary>
        public string TargetDrive { get; set; } = "";

        /// <summary>
        /// 移动时间限制（分钟）
        /// 如果文件最后修改时间与当前时间的差值在此限制范围内，则跳过移动
        /// 默认值为0，表示不启用时间限制
        /// </summary>
        public int MoveTimeLimitMinutes { get; set; } = 0;
    }

    public class AutoMoveFile
    {
        private static readonly string ConfigDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirPath, "AutoMoveFile.json");
        private static readonly string MoveRecordFileName = "移动记录.txt";
        private static List<AutoMoveConfig> _configs = new List<AutoMoveConfig>();
        private static bool _isRunning = false;

        /// <summary>
        /// 开始自动移动文件
        /// </summary>
        public static void Start()
        {
            LogHelper.Logger.Information("自动移动文件开始");
            Task.Run(Run);
        }

        /// <summary>
        /// 执行移动任务
        /// </summary>
        private static void Run()
        {
            _isRunning = true;
            List<AutoMoveConfig> configCache = null;
            int configCheckInterval = 0;
            const int configCheckFrequency = 2; // 每2次循环检查一次配置（约20秒）
            
            while (_isRunning)
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

                    // 执行每个配置的移动任务
                    if (configCache != null && configCache.Count > 0)
                    {
                        foreach (var config in configCache)
                        {
                            ExecuteMoveTask(config);
                        }
                        LogHelper.Logger.Information("自动移动文件任务完成");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Logger.Error($"自动移动文件异常：{ex.Message}", ex);
                }
                finally
                {
                    Thread.Sleep(1000 * 10); // 每10秒执行一次
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
                _configs = JsonSerializer.Deserialize<List<AutoMoveConfig>>(json, options) ?? new List<AutoMoveConfig>();

                LogHelper.Logger.Information($"读取移动配置文件成功，共{_configs.Count}个配置");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"读取移动配置文件异常：{ex.Message}", ex);
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
                var defaultConfigs = new List<AutoMoveConfig>
                {
                    new AutoMoveConfig
                    {
                        SourceDirectory = "D:\\MoveSource",
                        TargetDrive = "E:",
                        MoveTimeLimitMinutes = 0
                    }
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(defaultConfigs, options);
                File.WriteAllText(ConfigFilePath, json, Encoding.UTF8);
                LogHelper.Logger.Information($"创建默认移动配置文件：{ConfigFilePath}");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"创建默认移动配置文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行移动任务
        /// </summary>
        /// <param name="config">移动配置</param>
        private static void ExecuteMoveTask(AutoMoveConfig config)
        {
            try
            {
                if (string.IsNullOrEmpty(config.SourceDirectory) || string.IsNullOrEmpty(config.TargetDrive))
                {
                    LogHelper.Logger.Warning("移动配置不完整，跳过该任务");
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
                var recordPath = GetMoveRecordPath();
                InitializeMoveRecord(recordPath);

                // 获取所有需要移动的文件
                var filesToMove = GetFilesToMove(config.SourceDirectory);
                
                // 如果没有文件需要移动，释放内存并返回
                if (filesToMove == null || filesToMove.Count == 0)
                {
                    LogHelper.Logger.Information($"源目录 {config.SourceDirectory} 没有需要移动的文件");
                    return;
                }

                int movedCount = 0;
                long totalMovedSize = 0;
                int skippedCount = 0;

                foreach (var file in filesToMove)
                {
                    try
                    {
                        // 检查是否在时间限制范围内
                        if (IsFileWithinTimeLimit(file, config.MoveTimeLimitMinutes))
                        {
                            skippedCount++;
                            // 不记录跳过的文件
                            continue;
                        }

                        var result = MoveFileWithStructure(file, config.SourceDirectory, config.TargetDrive, recordPath);
                        if (result.success)
                        {
                            movedCount++;
                            totalMovedSize += result.fileSize;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Logger.Error($"处理文件异常：{file}，错误：{ex.Message}");
                    }
                }

                LogHelper.Logger.Information($"移动任务完成，从 {config.SourceDirectory} 到 {config.TargetDrive}，共移动 {movedCount} 个文件({totalMovedSize / (1024.0 * 1024.0):F2}MB)，跳过 {skippedCount} 个文件");
                
                // 释放文件列表内存
                filesToMove.Clear();
                filesToMove = null;
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"执行移动任务异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取需要移动的文件列表
        /// </summary>
        /// <param name="directory">目录路径</param>
        /// <returns>文件路径列表</returns>
        private static List<string> GetFilesToMove(string directory)
        {
            var filesToMove = new List<string>();

            try
            {
                var allFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    var fileName = Path.GetFileName(file);

                    // 排除移动记录文件
                    if (fileName.StartsWith("移动记录", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    filesToMove.Add(file);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"获取文件列表异常：{ex.Message}", ex);
            }

            return filesToMove;
        }

        /// <summary>
        /// 获取移动记录文件路径（CSV格式）
        /// </summary>
        /// <returns>记录文件路径</returns>
        private static string GetMoveRecordPath()
        {
            string recordDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "record", "AutoMoveFile");
            string today = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(recordDir, $"{today}.csv");
        }

        /// <summary>
        /// 初始化移动记录文件
        /// </summary>
        /// <param name="recordPath">记录文件路径</param>
        private static void InitializeMoveRecord(string recordPath)
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
                    LogHelper.Logger.Information($"创建移动记录文件：{recordPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"初始化移动记录文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 移动文件并保持目录结构
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceRootDirectory">源根目录</param>
        /// <param name="targetDrive">目标磁盘</param>
        /// <param name="recordPath">记录文件路径</param>
        /// <returns>移动结果</returns>
        private static (bool success, long fileSize) MoveFileWithStructure(string sourceFilePath, string sourceRootDirectory, string targetDrive, string recordPath)
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

                // 移动文件
                var fileInfo = new FileInfo(sourceFilePath);
                File.Move(sourceFilePath, targetFilePath, true);

                // 删除源目录中的空目录
                try
                {
                    string sourceDir = Path.GetDirectoryName(sourceFilePath) ?? "";
                    while (sourceDir != sourceRootDirectory && Directory.Exists(sourceDir) && Directory.GetFileSystemEntries(sourceDir).Length == 0)
                    {
                        Directory.Delete(sourceDir);
                        sourceDir = Path.GetDirectoryName(sourceDir) ?? "";
                    }
                }
                catch { }

                // 记录到CSV
                WriteMoveRecord(recordPath, sourceFilePath, targetFilePath);

                LogHelper.Logger.Information($"文件移动成功：{sourceFilePath} -> {targetFilePath}");
                return (true, fileInfo.Length);
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"移动文件失败：{sourceFilePath}，错误：{ex.Message}");
                return (false, 0);
            }
        }

        /// <summary>
        /// 写入移动记录
        /// </summary>
        private static void WriteMoveRecord(string recordPath, string sourceFile, string targetFile)
        {
            try
            {
                var record = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},\"{sourceFile}\",\"{targetFile}\"";
                File.AppendAllText(recordPath, record + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"写入移动记录异常：{ex.Message}", ex);
            }
        }



        /// <summary>
        /// 检查文件是否在移动时间限制范围内
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="timeLimitMinutes">时间限制（分钟）</param>
        /// <returns>如果文件在时间限制范围内返回true，否则返回false</returns>
        private static bool IsFileWithinTimeLimit(string filePath, int timeLimitMinutes)
        {
            try
            {
                // 如果时间限制为0或负数，表示不启用时间限制
                if (timeLimitMinutes <= 0)
                {
                    return false;
                }

                var fileInfo = new FileInfo(filePath);
                var lastWriteTime = fileInfo.LastWriteTime;
                var currentTime = DateTime.Now;
                var timeDifference = currentTime - lastWriteTime; 
                // 如果时间差小于等于限制时间，则在时间限制范围内
                return timeDifference.TotalMinutes <= timeLimitMinutes;
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"检查文件时间限制异常 {filePath}：{ex.Message}", ex);
                return false; // 异常情况下不限制移动
            }
        }

        /// <summary>
        /// 文件移动结果
        /// </summary>
        public struct MoveResult
        {
            public bool moved;
            public bool skipped;
            public string reason;

            public MoveResult(bool moved, bool skipped, string reason = "")
            {
                this.moved = moved;
                this.skipped = skipped;
                this.reason = reason;
            }
        }

        /// <summary>
        /// 移动单个文件
        /// </summary>
        /// <param name="sourceFile">源文件路径</param>
        /// <param name="sourceRoot">源根目录</param>
        /// <param name="targetRoot">目标根目录</param>
        /// <param name="recordPath">记录文件路径</param>
        /// <param name="config">移动配置</param>
        /// <returns>移动结果</returns>
        private static MoveResult MoveFile(string sourceFile, string sourceRoot, string targetRoot, string recordPath, AutoMoveConfig config)
        {
            try
            {
                // 检查文件是否在时间限制范围内
                if (IsFileWithinTimeLimit(sourceFile, config.MoveTimeLimitMinutes))
                {
                    var fileInfo = new FileInfo(sourceFile);
                    var timeDifference = DateTime.Now - fileInfo.LastWriteTime;
                    var skippedReason = $"文件在时间限制范围内（最后修改时间：{fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}，距离现在：{timeDifference.TotalMinutes:F1}分钟）";
                    
                    LogHelper.Logger.Information($"跳过移动文件：{Path.GetRelativePath(sourceRoot, sourceFile)} - {skippedReason}");
                    
                    
                    return new MoveResult(false, true, skippedReason);
                }

                // 计算相对路径，保持目录结构
                var relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
                var targetFile = Path.Combine(targetRoot, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);

                // 确保目标目录存在
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // 如果目标文件已存在，生成新文件名
                if (File.Exists(targetFile))
                {
                    targetFile = GenerateUniqueFileName(targetFile);
                }

                // 执行移动
                File.Move(sourceFile, targetFile);
                LogHelper.Logger.Information($"移动文件：{relativePath}");

                // 记录移动操作
                WriteMoveRecord(recordPath, sourceFile, targetFile);

                return new MoveResult(true, false, "移动成功");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"移动文件异常 {sourceFile}：{ex.Message}", ex);

                return new MoveResult(false, false, ex.Message);
            }
        }

        /// <summary>
        /// 生成唯一文件名
        /// </summary>
        /// <param name="filePath">原文件路径</param>
        /// <returns>唯一文件路径</returns>
        private static string GenerateUniqueFileName(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            int counter = 1;
            string newFilePath;

            do
            {
                var newFileName = $"{fileNameWithoutExtension}({counter}){extension}";
                newFilePath = Path.Combine(directory!, newFileName);
                counter++;
            }
            while (File.Exists(newFilePath));

            return newFilePath;
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的文件大小</returns>
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 停止自动移动
        /// </summary>
        public static void Stop()
        {
            _isRunning = false;
            LogHelper.Logger.Information("自动移动文件停止");
        }
    }
}