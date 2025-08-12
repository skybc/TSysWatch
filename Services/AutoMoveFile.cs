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

# 可以添加更多移动任务
#[MoveTask]
#SourceDirectory=C:\TempFiles
#TargetDirectory=D:\Archive
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
                foreach (var file in filesToMove)
                {
                    if (MoveFile(file, config.SourceDirectory, config.TargetDirectory, recordPath))
                    {
                        movedCount++;
                    }
                }

                LogHelper.Logger.Information($"移动任务完成，从 {config.SourceDirectory} 到 {config.TargetDirectory}，共移动 {movedCount} 个文件");
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
        private static void WriteMoveRecord(string recordPath, string operation, string sourceFile, string targetFile,  string status)
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
        /// 移动单个文件
        /// </summary>
        /// <param name="sourceFile">源文件路径</param>
        /// <param name="sourceRoot">源根目录</param>
        /// <param name="targetRoot">目标根目录</param>
        /// <param name="recordPath">记录文件路径</param>
        /// <returns>是否移动成功</returns>
        private static bool MoveFile(string sourceFile, string sourceRoot, string targetRoot, string recordPath)
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

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"移动文件异常 {sourceFile}：{ex.Message}", ex);
                
                // 记录失败操作
                try
                {
                    var fileInfo = new FileInfo(sourceFile);
                    WriteMoveRecord(recordPath, "移动", sourceFile, "失败", ex.Message);
                }
                catch { }
                
                return false;
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