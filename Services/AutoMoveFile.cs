using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        /// 移动根目录（源目录）
        /// </summary>
        public string SourceDirectory { get; set; } = "";

        /// <summary>
        /// 目标目录
        /// </summary>
        public string TargetDirectory { get; set; } = "";

        /// <summary>
        /// 移动时间限制（分钟）
        /// 如果文件最后修改时间与当前时间的差值在此限制范围内，则跳过移动
        /// 默认值为0，表示不启用时间限制
        /// </summary>
        public int MoveTimeLimitMinutes { get; set; } = 0;
    }

    public class AutoMoveFile
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoMoveFile.ini");
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
            while (_isRunning)
            {
                try
                {
                    // 读取配置文件
                    ReadIniFile();

                    // 执行每个配置的移动任务
                    foreach (var config in _configs)
                    {
                        ExecuteMoveTask(config);
                    }

                    LogHelper.Logger.Information("自动移动文件任务完成");
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
                AutoMoveConfig? currentConfig = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        // 新的移动配置节
                        if (currentConfig != null)
                        {
                            _configs.Add(currentConfig);
                        }
                        currentConfig = new AutoMoveConfig();
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
                                case "movetimelimitminutes":
                                    if (int.TryParse(value, out int limitMinutes))
                                    {
                                        currentConfig.MoveTimeLimitMinutes = limitMinutes;
                                    }
                                    else
                                    {
                                        LogHelper.Logger.Warning($"无效的移动时间限制配置值：{value}，使用默认值0");
                                    }
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

                LogHelper.Logger.Information($"读取移动配置文件成功，共{_configs.Count}个配置");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"读取移动配置文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        private static void CreateDefaultIniFile()
        {
            try
            {
                var defaultContent = @"# 自动移动文件配置
# 支持配置多个移动任务，每个任务使用[MoveTask]开始

[MoveTask]
# 移动根目录（源目录） - 要移动的文件所在目录
SourceDirectory=D:\MoveSource
# 目标目录 - 移动到的目标目录
TargetDirectory=E:\MoveTarget
# 移动时间限制（分钟）- 如果文件最后修改时间与当前时间差值在此限制内则跳过移动
# 设置为0表示不启用时间限制，所有文件都会被移动
MoveTimeLimitMinutes=0

# 可以添加更多移动任务
#[MoveTask]
#SourceDirectory=C:\TempFiles
#TargetDirectory=D:\Archive
#MoveTimeLimitMinutes=60
";

                File.WriteAllText(ConfigFilePath, defaultContent, Encoding.UTF8);
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
                if (string.IsNullOrEmpty(config.SourceDirectory) || string.IsNullOrEmpty(config.TargetDirectory))
                {
                    LogHelper.Logger.Warning("移动配置不完整，跳过该任务");
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

                // 获取移动记录文件路径
                var recordPath = Path.Combine(config.SourceDirectory, "移动记录_" + DateTime.Now.ToString("yyyyMMdd") + ".txt");
                InitializeMoveRecord(recordPath);

                // 获取所有需要移动的文件
                var filesToMove = GetFilesToMove(config.SourceDirectory);

                int movedCount = 0;
                int skippedCount = 0;
                foreach (var file in filesToMove)
                {
                    var result = MoveFile(file, config.SourceDirectory, config.TargetDirectory, recordPath, config);
                    if (result.moved)
                    {
                        movedCount++;
                    }
                    else if (result.skipped)
                    {
                        skippedCount++;
                    }
                }

                LogHelper.Logger.Information($"移动任务完成，从 {config.SourceDirectory} 到 {config.TargetDirectory}，共移动 {movedCount} 个文件，跳过 {skippedCount} 个文件");
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
        /// 初始化移动记录文件
        /// </summary>
        /// <param name="recordPath">记录文件路径</param>
        private static void InitializeMoveRecord(string recordPath)
        {
            try
            {
                if (!File.Exists(recordPath))
                {
                    var header = "=== 文件移动记录 ===" + Environment.NewLine +
                               "格式：[时间] 操作 - 源文件路径 => 目标文件路径 (文件大小)" + Environment.NewLine +
                               Environment.NewLine;
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
        /// 写入移动记录
        /// </summary>
        /// <param name="recordPath">记录文件路径</param>
        /// <param name="operation">操作类型</param>
        /// <param name="sourceFile">源文件路径</param>
        /// <param name="targetFile">目标文件路径</param>
        /// <param name="fileSize">文件大小</param>
        /// <param name="status">操作状态</param>
        private static void WriteMoveRecord(string recordPath, string operation, string sourceFile, string targetFile, string status)
        {
            try
            {
                var record = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {operation} - {sourceFile} => {targetFile} - {status}";
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
                    
                    // 记录跳过操作
                    WriteMoveRecord(recordPath, "跳过", sourceFile, "N/A", skippedReason);
                    
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
                WriteMoveRecord(recordPath, "移动", sourceFile, targetFile, "成功");

                return new MoveResult(true, false, "移动成功");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"移动文件异常 {sourceFile}：{ex.Message}", ex);

                // 记录失败操作
                try
                {
                    WriteMoveRecord(recordPath, "移动", sourceFile, "失败", ex.Message);
                }
                catch { }

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