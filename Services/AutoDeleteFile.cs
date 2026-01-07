using Dm.util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TSysWatch
{

    public class AutoDeleteFile
    {
        private static readonly string ConfigDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirPath, "AutoDeleteFile.json");
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
            List<DiskCleanupConfig> configCache = null;
            int configCheckInterval = 0;
            const int configCheckFrequency = 10; // 每10次循环检查一次配置
            
            while (_isRunning)
            {
                try
                {
                    // 定期重新读取配置（每10次循环检查一次）
                    if (configCheckInterval++ >= configCheckFrequency)
                    {
                        ReadJsonFile();
                        configCache = _configs;
                        configCheckInterval = 0;
                        
                        if (configCache.isEmpty())
                        {
                            // 建立文件索引
                            foreach (var config in configCache)
                            {
                                BuildIndex(configCache);
                            }
                        }
                    }

                    // 使用缓存的配置检查磁盘
                    if (configCache != null)
                    {
                        foreach (var config in configCache)
                        {
                            CheckAndCleanDisk(config);
                        }
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

        private static void BuildIndex(List<DiskCleanupConfig> configs)
        {
             
        }

        /// <summary>
        /// 读取INI配置文件
        /// </summary>
        /// <summary>
        /// 从JSON配置文件读取配置
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
                _configs = JsonSerializer.Deserialize<List<DiskCleanupConfig>>(json, options) ?? new List<DiskCleanupConfig>();

                LogHelper.Logger.Information($"读取JSON配置文件成功，共{_configs.Count}个磁盘配置");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"读取JSON配置文件异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 确保配置目录存在
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
        /// 创建默认INI配置文件
        /// </summary>
        /// <summary>
        /// 创建默认JSON配置文件
        /// </summary>
        private static void CreateDefaultJsonFile()
        {
            try
            {
                EnsureConfigDirectory();
                var defaultConfigs = new List<DiskCleanupConfig>
                {
                    new DiskCleanupConfig
                    {
                        DriveLetter = "C:",
                        DeleteDirectories = new List<string> { "C:\\Temp", "C:\\Logs", "C:\\Cache" },
                        StartDeleteSizeGB = 5.0,
                        StopDeleteSizeGB = 10.0,
                        StartDeleteFileDays = 30,
                        LogicMode = DeleteLogicMode.OR
                    },
                    new DiskCleanupConfig
                    {
                        DriveLetter = "D:",
                        DeleteDirectories = new List<string> { "D:\\Temp", "D:\\Logs" },
                        StartDeleteSizeGB = 10.0,
                        StopDeleteSizeGB = 20.0,
                        StartDeleteFileDays = 7,
                        LogicMode = DeleteLogicMode.AND
                    }
                };

                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(defaultConfigs, options);
                File.WriteAllText(ConfigFilePath, json, System.Text.Encoding.UTF8);
                LogHelper.Logger.Information($"创建默认JSON配置文件：{ConfigFilePath}");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"创建默认JSON配置文件异常：{ex.Message}", ex);
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
                // 检查容量条件,如果逻辑模式是AND，且剩余空间小于开始删除容量，则不进行删除
                if (config.LogicMode == DeleteLogicMode.AND)
                {
                    if (freeSpaceGB > config.StartDeleteSizeGB)
                    {
                        LogHelper.Logger.Information($"当前为AND模式，磁盘 {config.DriveLetter} 剩余空间不足：{freeSpaceGB:F2}GB < {config.StartDeleteSizeGB}GB");
                        return;
                    }
                }
                LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 剩余空间：{freeSpaceGB:F2}GB，配置：容量阈值{config.StartDeleteSizeGB}GB，时间阈值{config.StartDeleteFileDays}天，逻辑模式{config.LogicMode}");

                // 获取所有候选删除文件
                DateTime dt = DateTime.Now.AddDays(-config.StartDeleteFileDays);

                var candidateFiles = GetFilesToDelete(config);
                var dt2 = DateTime.Now;

                // 如果容量条件满足，按时间排序（最旧的文件先删除） 
                LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 开始清理");
                int deletedCount = 0;
                long totalDeletedSize = 0;
                var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot/record/auto_delete", dt2.ToString("yyyy/MM"), $"{dt2.ToString("yyyyMMdd")}.txt");
                // create
                var logDirectory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                // 检查文件是否存在
                if (!File.Exists(logFilePath))
                {
                    // 创建日志文件
                    File.Create(logFilePath).Dispose();
                }

                // 收集日志消息，最后一次性写入，避免频繁打开文件
                var logMessages = new List<string>();
                
                // 遍历候选文件列表，按时间排序
                LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 找到 {candidateFiles.Count} 个候选文件，开始删除");
                foreach (var file in candidateFiles.OrderBy(f => f.LastWriteTime))
                {
                    try
                    {
                        // 检查文件是否可以删除
                        var dr = AutoDeleteFileManager.ShouldDeleteFile(config, freeSpaceGB, file, dt);
                        // 如果不可以删除，停止删除
                        if (!dr.CanDelete)
                        {
                            // 停止删除
                            LogHelper.Logger.Information($"停止删除{file.FullName}，原因：{dr.Reason}");
                            break;
                        }
                        // 删除文件
                        long fileSize = file.Length;
                        // 计算文件年龄
                        var fileAge = DateTime.Now - file.LastWriteTime;
                        // 更新剩余空间
                        freeSpaceGB += fileSize / (1024.0 * 1024.0 * 1024.0);
                        File.Delete(file.FullName);
                        // 打印
                        deletedCount++;
                        totalDeletedSize += file.Length;
                        // 删除文件单独日志，每天一个，放到：当前应用目录+/record/auto_delete/yyyy/MM/yyyyMMdd.txt里面
                        string log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 删除文件：{file.FullName}，大小：{fileSize / (1024.0 * 1024.0):F2}MB,文件修改日期：{file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")}，原因：{dr.Reason}，文件年龄：{fileAge.Days}天";
                        logMessages.Add(log);
                        LogHelper.Logger.Information(log);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Logger.Error($"删除文件失败：{file.FullName}，错误：{ex.Message}");
                    }
                }
                
                // 一次性写入所有日志消息
                if (logMessages.Count > 0)
                {
                    using (var stream = new StreamWriter(logFilePath, true))
                    {
                        foreach (var log in logMessages)
                        {
                            stream.WriteLine(log);
                        }
                        stream.Flush();
                    }
                    logMessages.Clear();
                }
                
                LogHelper.Logger.Information($"磁盘 {config.DriveLetter} 清理完成，删除了 {deletedCount} 个文件，总大小：{totalDeletedSize / (1024.0 * 1024.0):F2}MB");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"检查磁盘异常：{config.DriveLetter}，错误：{ex.Message}", ex);
            }
        }
        public class FileEx
        {
            /// <summary>
            /// 完整文件路径
            /// </summary>
            public string FullName { get; set; } = "";

            /// <summary>
            /// 最后写入时间
            /// </summary>
            public DateTime LastWriteTime { get; set; }

            /// <summary>
            /// 文件大小（字节）
            /// </summary>
            public int Length { get; set; }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindFirstFileW(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool FindNextFileW(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose(IntPtr hFindFile);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="result"></param>
        private static void EnumerateFilesFast(string folder, ConcurrentBag<FileEx> result)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                WIN32_FIND_DATA findData;
                IntPtr hFind = FindFirstFileW(Path.Combine(folder, "*"), out findData);

                if (hFind.ToInt64() == -1)
                    return;

                try
                {
                    do
                    {
                        string name = findData.cFileName;

                        if (name == "." || name == "..")
                            continue;

                        bool isDir = (findData.dwFileAttributes & FileAttributes.Directory) != 0;

                        if (isDir)
                        {
                            // 如果是目录，递归遍历子目录
                            EnumerateFilesFast(Path.Combine(folder, name), result);
                        }
                        else
                        {
                            long size = ((long)findData.nFileSizeHigh << 32) + findData.nFileSizeLow;
                            DateTime lastWriteTime = DateTime.FromFileTimeUtc(
                                ((long)findData.ftLastWriteTime.dwHighDateTime << 32) |
                                (uint)findData.ftLastWriteTime.dwLowDateTime);

                            result.Add(new FileEx()
                            {
                                FullName = Path.Combine(folder, name),
                                LastWriteTime = lastWriteTime,
                                Length = (int)size
                            });

                        }
                    }
                    while (FindNextFileW(hFind, out findData));
                }
                finally
                {
                    FindClose(hFind);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"遍历目录 {folder} 时发生错误：{ex.Message}", ex);
            }
            finally
            {
                stopwatch.Stop();
                LogHelper.Logger.Information($"遍历目录 {folder} 耗时：{stopwatch.ElapsedMilliseconds}ms");
            }
        }


        /// <summary>
        /// 获取要删除的文件列表
        /// </summary>
        /// <param name="config">磁盘配置</param>
        /// <returns>文件信息列表</returns>
        private static ConcurrentBag<FileEx> GetFilesToDelete(DiskCleanupConfig config)
        {
            Stopwatch stopWatch = new Stopwatch();
            var files = new ConcurrentBag<FileEx>();
            // 并行
            config.DeleteDirectories.AsParallel().ForAll(d =>
            {
                EnumerateFilesFast(d, files);
            });
            //foreach (var directory in config.DeleteDirectories)
            //{
            //    stopWatch.Start();
            //    try
            //    {
            //        EnumerateFilesFast(directory, files);
            //        //// 目录需要和磁盘驱动器相同
            //        //if (!directory.StartsWith(config.DriveLetter, StringComparison.OrdinalIgnoreCase))
            //        //{
            //        //    LogHelper.Logger.Warning($"目录 {directory} 不在磁盘 {config.DriveLetter} 下，跳过");
            //        //    continue;
            //        //}

            //        //if (!Directory.Exists(directory))
            //        //{
            //        //    LogHelper.Logger.Warning($"删除目录不存在：{directory}");
            //        //    continue;
            //        //}

            //        //var directoryInfo = new DirectoryInfo(directory);

            //        //// 递归获取所有文件（包括子目录）
            //        //var directoryFiles = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
            //        //// log
            //        //LogHelper.Logger.Information($"目录 {directory} 找到 {directoryFiles.Length} 个文件");
            //        //// 转换为 FileEx 对象
            //        //files.AddRange(directoryFiles.Select(f => new FileEx
            //        //{
            //        //    FullName = f.FullName,
            //        //    LastWriteTime = f.LastWriteTime,
            //        //    Length = (int)f.Length
            //        //}));
            //        //files.AddRange(directoryFiles);

            //    }
            //    catch (Exception ex)
            //    {
            //        LogHelper.Logger.Error($"获取目录文件失败：{directory}，错误：{ex.Message}");
            //    }
            //    finally
            //    {
            //        stopWatch.Stop();
            //        LogHelper.Logger.Information($"获取目录{directory}耗时：{stopWatch.ElapsedMilliseconds}ms");
            //    }
            //}
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
