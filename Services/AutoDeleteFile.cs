using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Dm.util;

namespace TSysWatch
{

    public class AutoDeleteFile
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoDeleteFile.ini");
        private static List<DiskCleanupConfig> _configs = new List<DiskCleanupConfig>();
        private static bool _isRunning = false;

        public static void Start()
        {
            // log
            LogHelper.Logger.Information("自动清理文件开始");
            Task.Run(Run);
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        /// <returns></returns>
        public static bool IsRunning()
        {
            return _isRunning;
        }

        /// <summary>
        /// 运行中
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
                    // 检查每个磁盘配置
                    foreach (var config in _configs)
                    {
                        CheckAndCleanDisk(config);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Logger.Error($"自动清理文件异常：{ex.Message}", ex);
                }
                finally
                {
                    Thread.Sleep(1000 * 60); // 每60秒检查一次
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
                DiskCleanupConfig currentConfig = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        // 新的磁盘配置节
                        if (currentConfig != null)
                        {
                            _configs.Add(currentConfig);
                        }
                        currentConfig = new DiskCleanupConfig
                        {
                            DriveLetter = trimmedLine.Substring(1, trimmedLine.Length - 2)
                        };
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
                                case "deletedirectories":
                                    currentConfig.DeleteDirectories = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(d => d.Trim()).ToList();
                                    break;
                                case "startdeletesizegb":
                                    if (double.TryParse(value, out double startSize))
                                        currentConfig.StartDeleteSizeGB = startSize;
                                    break;
                                case "stopdeletesizegb":
                                    if (double.TryParse(value, out double stopSize))
                                        currentConfig.StopDeleteSizeGB = stopSize;
                                    break;
                                case "startdeletefiledays":
                                    if (int.TryParse(value, out int fileDays))
                                        currentConfig.StartDeleteFileDays = fileDays;
                                    break;
                                case "logicmode":
                                    if (Enum.TryParse<DeleteLogicMode>(value, true, out DeleteLogicMode mode))
                                        currentConfig.LogicMode = mode;
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

                LogHelper.Logger.Information($"读取配置文件成功，共{_configs.Count}个磁盘配置");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"读取配置文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建默认INI配置文件
        /// </summary>
        private static void CreateDefaultIniFile()
        {
            try
            {
                var defaultConfig = @"# 自动删除文件配置
# 格式：[磁盘驱动器]
# DeleteDirectories=目录1,目录2,目录3
# StartDeleteSizeGB=开始删除时的磁盘剩余空间(GB)
# StopDeleteSizeGB=停止删除时的磁盘剩余空间(GB)
# StartDeleteFileDays=开始删除文件时间(天) - 只删除超过N天的文件，0表示不限制时间
# LogicMode=删除条件逻辑关系 - AND(且)/OR(或)，AND表示同时满足容量和时间条件，OR表示满足任一条件

[C:]
DeleteDirectories=C:\temp,C:\logs,C:\cache
StartDeleteSizeGB=5.0
StopDeleteSizeGB=10.0
StartDeleteFileDays=30
LogicMode=OR

[D:]
DeleteDirectories=D:\temp,D:\logs
StartDeleteSizeGB=10.0
StopDeleteSizeGB=20.0
StartDeleteFileDays=7
LogicMode=AND
";

                File.WriteAllText(ConfigFilePath, defaultConfig, Encoding.UTF8);
                LogHelper.Logger.Information($"创建默认配置文件：{ConfigFilePath}");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"创建默认配置文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查并清理磁盘
        /// </summary>
        /// <param name="config">磁盘配置</param>
        private static void CheckAndCleanDisk(DiskCleanupConfig config)
        {
            try
            {
                if (!Directory.Exists(config.DriveLetter))
                {
                    LogHelper.Logger.Warning($"磁盘驱动器不存在：{config.DriveLetter}");
                    return;
                }

                var driveInfo = new DriveInfo(config.DriveLetter);
                if (!driveInfo.IsReady)
                {
                    LogHelper.Logger.Warning($"磁盘驱动器未准备好：{config.DriveLetter}");
                    return;
                }

                double freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

                LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 剩余空间：{freeSpaceGB:F2}GB，配置：容量阈值{config.StartDeleteSizeGB}GB，时间阈值{config.StartDeleteFileDays}天，逻辑模式{config.LogicMode}");

                // 获取所有候选删除文件
                var candidateFiles = GetFilesToDelete(config);

                // 筛选需要删除的文件（根据新的逻辑条件）
                List<DeleteReason> filesToDelete = candidateFiles.Select(file =>
                  AutoDeleteFileManager.ShouldDeleteFile(config, freeSpaceGB, file)
                ).Where(r => r.CanDelete).ToList();

                if (filesToDelete.Count == 0)
                {
                    LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 没有符合删除条件的文件");
                    return;
                }

                // 如果容量条件满足，按时间排序（最旧的文件先删除）
                var sortedFiles = filesToDelete.OrderBy(f => f.FileInfo.LastWriteTime).ToList();

                LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 开始清理，共找到 {sortedFiles.Count} 个符合删除条件的文件");
                int deletedCount = 0;
                long totalDeletedSize = 0;

                foreach (var file in sortedFiles)
                {
                    try
                    {
                        // 如果是容量驱动的删除，需要实时检查磁盘空间
                        if (config.LogicMode == DeleteLogicMode.OR || freeSpaceGB < config.StartDeleteSizeGB)
                        {
                            // 再次检查磁盘空间
                            driveInfo = new DriveInfo(config.DriveLetter);
                            freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

                            // 如果空间已经足够且是容量驱动模式，停止删除
                            if (freeSpaceGB >= config.StopDeleteSizeGB &&
                                (config.LogicMode == DeleteLogicMode.OR && config.StartDeleteFileDays == 0))
                            {
                                LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 空间已恢复到 {freeSpaceGB:F2}GB，停止清理");
                                break;
                            }
                        }

                        // 删除文件
                        long fileSize = file.FileInfo.Length;
                        var fileAge = DateTime.Now - (file.FileInfo.CreationTime > file.FileInfo.LastWriteTime ? file.FileInfo.CreationTime : file.FileInfo.LastWriteTime);
                        // 打印

                        File.Delete(file.FileInfo.FullName);
                        deletedCount++;
                        totalDeletedSize += fileSize;
                        // 删除文件单独日志，每天一个，放到：当前应用目录+/record/auto_delete/yyyy/MM/yyyyMMdd.txt里面
                        LogHelper.Logger.Information($"删除文件：{file.FileInfo.FullName}，原因：{file.Reason}，大小：{fileSize / (1024.0 * 1024.0):F2}MB，文件年龄：{fileAge.Days}天");
                        var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot/record/auto_delete", file.FileInfo.LastWriteTime.ToString("yyyy/MM"), $"{file.FileInfo.LastWriteTime:yyyyMMdd}.txt");
                        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                        File.AppendAllText(logFilePath, $"删除文件：{file.FileInfo.FullName}，原因：{file.Reason}，大小：{fileSize / (1024.0 * 1024.0):F2}MB，文件年龄：{fileAge.Days}天{Environment.NewLine}");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Logger.Error($"删除文件失败：{file.FileInfo.FullName}，错误：{ex.Message}");
                    }
                }

                LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 清理完成，删除了 {deletedCount} 个文件，总大小：{totalDeletedSize / (1024.0 * 1024.0):F2}MB");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"检查磁盘异常：{config.DriveLetter}，错误：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取要删除的文件列表
        /// </summary>
        /// <param name="config">磁盘配置</param>
        /// <returns>文件信息列表</returns>
        private static List<FileInfo> GetFilesToDelete(DiskCleanupConfig config)
        {
            var files = new List<FileInfo>();

            foreach (var directory in config.DeleteDirectories)
            {
                try
                {
                    // 目录需要和磁盘驱动器相同
                    if (!directory.StartsWith(config.DriveLetter, StringComparison.OrdinalIgnoreCase))
                    {
                        LogHelper.Logger.Warning($"目录 {directory} 不在磁盘 {config.DriveLetter} 下，跳过");
                        continue;
                    }

                    if (!Directory.Exists(directory))
                    {
                        LogHelper.Logger.Warning($"删除目录不存在：{directory}");
                        continue;
                    }

                    var directoryInfo = new DirectoryInfo(directory);

                    // 递归获取所有文件（包括子目录）
                    var directoryFiles = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
                    files.AddRange(directoryFiles);

                    LogHelper.Logger.Information($"目录 {directory} 找到 {directoryFiles.Length} 个文件");
                }
                catch (Exception ex)
                {
                    LogHelper.Logger.Error($"获取目录文件失败：{directory}，错误：{ex.Message}");
                }
            }
            return files;
        }

        /// <summary>
        /// 停止自动清理
        /// </summary>
        public static void Stop()
        {
            _isRunning = false;
            LogHelper.Logger.Information("自动清理文件停止");
        }
    }
}
