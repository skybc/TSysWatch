using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

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

[C:]
DeleteDirectories=C:\temp,C:\logs,C:\cache
StartDeleteSizeGB=5.0
StopDeleteSizeGB=10.0

[D:]
DeleteDirectories=D:\temp,D:\logs
StartDeleteSizeGB=10.0
StopDeleteSizeGB=20.0
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
                
                LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 剩余空间：{freeSpaceGB:F2}GB");

                // 检查是否需要开始清理
                if (freeSpaceGB <= config.StartDeleteSizeGB)
                {
                    LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 剩余空间不足，开始清理文件");
                    
                    // 获取所有要删除的文件
                    var filesToDelete = GetFilesToDelete(config);
                    
                    // 按时间排序（最旧的文件先删除）
                    var sortedFiles = filesToDelete.OrderBy(f => f.LastWriteTime).ToList();
                    
                    foreach (var file in sortedFiles)
                    {
                        try
                        {
                            // 再次检查磁盘空间
                            driveInfo = new DriveInfo(config.DriveLetter);
                            freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                            
                            if (freeSpaceGB >= config.StopDeleteSizeGB)
                            {
                                LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 空间已恢复到 {freeSpaceGB:F2}GB，停止清理");
                                break;
                            }
                            
                            // 删除文件
                            long fileSize = file.Length;
                            File.Delete(file.FullName);
                            LogHelper.Logger.Information($"删除文件：{file.FullName}，大小：{fileSize / (1024.0 * 1024.0):F2}MB");
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Logger.Error($"删除文件失败：{file.FullName}，错误：{ex.Message}");
                        }
                    }
                }
                else
                {
                    // 空间充足，无需清理
                    LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 空间充足，无需清理");
                }
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
