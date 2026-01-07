using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using static TSysWatch.AutoDeleteFile;

namespace TSysWatch
{
    /// <summary>
    /// 自动删除文件管理工具
    /// </summary>
    public static class AutoDeleteFileManager
    {
        private static readonly string ConfigDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirPath, "AutoDeleteFile.json");

        /// <summary>
        /// 从JSON文件获取当前配置
        /// </summary>
        /// <returns>配置列表</returns>
        public static List<DiskCleanupConfig> GetCurrentConfigs()
        {
            var configs = new List<DiskCleanupConfig>();

            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    LogHelper.Logger.Warning("配置文件不存在");
                    return configs;
                }

                var json = File.ReadAllText(ConfigFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                configs = JsonSerializer.Deserialize<List<DiskCleanupConfig>>(json, options) ?? new List<DiskCleanupConfig>();

                LogHelper.Logger.Information($"读取配置文件成功，共{configs.Count}个磁盘配置");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"获取配置异常：{ex.Message}", ex);
            }

            return configs;
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
        /// 保存配置到JSON文件
        /// </summary>
        /// <param name="configs">配置列表</param>
        public static void SaveConfigs(List<DiskCleanupConfig> configs)
        {
            try
            {
                EnsureConfigDirectory();
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(configs, options);
                File.WriteAllText(ConfigFilePath, json, System.Text.Encoding.UTF8);
                LogHelper.Logger.Information("配置保存成功");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"保存配置异常：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 添加或更新磁盘配置
        /// </summary>
        /// <param name="driveLetter">驱动器字母</param>
        /// <param name="deleteDirectories">删除目录列表</param>
        /// <param name="startDeleteSizeGB">开始删除大小(GB)</param>
        /// <param name="stopDeleteSizeGB">停止删除大小(GB)</param>
        /// <param name="startDeleteFileDays">开始删除文件时间(天)</param>
        /// <param name="logicMode">删除条件逻辑关系</param>
        public static void AddOrUpdateConfig(string driveLetter, List<string> deleteDirectories, double startDeleteSizeGB, double stopDeleteSizeGB, int startDeleteFileDays = 0, DeleteLogicMode logicMode = DeleteLogicMode.OR)
        {
            var configs = GetCurrentConfigs();
            var existingConfig = configs.FirstOrDefault(c => c.DriveLetter.Equals(driveLetter, StringComparison.OrdinalIgnoreCase));

            if (existingConfig != null)
            {
                existingConfig.DeleteDirectories = deleteDirectories;
                existingConfig.StartDeleteSizeGB = startDeleteSizeGB;
                existingConfig.StopDeleteSizeGB = stopDeleteSizeGB;
                existingConfig.StartDeleteFileDays = startDeleteFileDays;
                existingConfig.LogicMode = logicMode;
            }
            else
            {
                configs.Add(new DiskCleanupConfig
                {
                    DriveLetter = driveLetter,
                    DeleteDirectories = deleteDirectories,
                    StartDeleteSizeGB = startDeleteSizeGB,
                    StopDeleteSizeGB = stopDeleteSizeGB,
                    StartDeleteFileDays = startDeleteFileDays,
                    LogicMode = logicMode
                });
            }

            SaveConfigs(configs);
        }

        /// <summary>
        /// 删除磁盘配置
        /// </summary>
        /// <param name="driveLetter">驱动器字母</param>
        public static void RemoveConfig(string driveLetter)
        {
            var configs = GetCurrentConfigs();
            configs.RemoveAll(c => c.DriveLetter.Equals(driveLetter, StringComparison.OrdinalIgnoreCase));
            SaveConfigs(configs);
        }

        /// <summary>
        /// 获取磁盘信息
        /// </summary>
        /// <returns>磁盘信息列表</returns>
        public static List<DriveInfo> GetDriveInfos()
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .ToList();
        }

        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        /// <param name="directories">目录列表</param>
        /// <returns>检查结果</returns>
        public static Dictionary<string, bool> CheckDirectoriesExist(List<string> directories)
        {
            var result = new Dictionary<string, bool>();

            foreach (var directory in directories)
            {
                result[directory] = Directory.Exists(directory);
            }

            return result;
        }

        /// <summary>
        /// 获取目录大小
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>目录大小（字节）</returns>
        public static long GetDirectorySize(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return 0;

                var directoryInfo = new DirectoryInfo(directoryPath);
                return directoryInfo.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"获取目录大小异常：{directoryPath}，错误：{ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 格式化字节大小
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的大小</returns>
        public static string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] orders = { "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return $"{decimal.Divide(bytes, max):##.##} {order}";

                max /= scale;
            }
            return "0 Bytes";
        }

       

        /// <summary>
        /// 检查是否应该删除文件（根据配置的逻辑关系判断）
        /// </summary>
        /// <param name="config">磁盘清理配置</param>
        /// <param name="currentFreeSpaceGB">当前磁盘剩余空间(GB)</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否应该删除文件</returns>
        public static DeleteReason ShouldDeleteFile(DiskCleanupConfig config, double currentFreeSpaceGB, FileEx fileInfo, DateTime dt)
        {
            // 检查容量条件
            bool capacityCondition = currentFreeSpaceGB < config.StopDeleteSizeGB; 
            // 检查时间条件
            bool timeCondition = config.StartDeleteFileDays < 0 ? true : dt > fileInfo.LastWriteTime;

            // 根据逻辑关系返回结果
            switch (config.LogicMode)
            {
                case DeleteLogicMode.AND:
                    // 且：必须同时满足容量和时间条件
                    return new DeleteReason
                    {
                        CanDelete = capacityCondition && timeCondition,
                        Reason = "同时满足容量和时间条件",
                        FileInfo = fileInfo
                    };
                case DeleteLogicMode.OR:
                    // 或：满足容量或时间条件之一即可
                    if (capacityCondition)
                    {
                        return new DeleteReason()
                        {
                            CanDelete = true,
                            Reason = "容量不足",
                            FileInfo = fileInfo
                        };
                    }
                    if (timeCondition)
                    {
                        return new DeleteReason()
                        {
                            CanDelete = true,
                            Reason = "文件过期",
                            FileInfo = fileInfo
                        };
                    }
                    else
                    {
                        return new DeleteReason()
                        {
                            CanDelete = false,
                            Reason = "不满足删除条件",
                            FileInfo = fileInfo
                        };
                    }
                default:
                    if (capacityCondition)
                    {
                        return new DeleteReason()
                        {
                            CanDelete = true,
                            Reason = "容量不足",
                            FileInfo = fileInfo
                        };
                    }
                    if (timeCondition)
                    {
                        return new DeleteReason()
                        {
                            CanDelete = true,
                            Reason = "文件过期",
                            FileInfo = fileInfo
                        };
                    }
                    else
                    {
                        return new DeleteReason()
                        {
                            CanDelete = false,
                            Reason = "不满足删除条件",
                            FileInfo = fileInfo
                        };
                    }
            }
        }
    }
}