using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TSysWatch
{
    /// <summary>
    /// 自动删除文件管理工具
    /// </summary>
    public static class AutoDeleteFileManager
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoDeleteFile.ini");

        /// <summary>
        /// 获取当前配置
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

                var lines = File.ReadAllLines(ConfigFilePath, Encoding.UTF8);
                DiskCleanupConfig currentConfig = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        if (currentConfig != null)
                        {
                            configs.Add(currentConfig);
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

                if (currentConfig != null)
                {
                    configs.Add(currentConfig);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"获取配置异常：{ex.Message}", ex);
            }

            return configs;
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="configs">配置列表</param>
        public static void SaveConfigs(List<DiskCleanupConfig> configs)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# 自动删除文件配置");
                sb.AppendLine("# 格式：[磁盘驱动器]");
                sb.AppendLine("# DeleteDirectories=目录1,目录2,目录3");
                sb.AppendLine("# StartDeleteSizeGB=开始删除时的磁盘剩余空间(GB)");
                sb.AppendLine("# StopDeleteSizeGB=停止删除时的磁盘剩余空间(GB)");
                sb.AppendLine();

                foreach (var config in configs)
                {
                    sb.AppendLine($"[{config.DriveLetter}]");
                    sb.AppendLine($"DeleteDirectories={string.Join(",", config.DeleteDirectories)}");
                    sb.AppendLine($"StartDeleteSizeGB={config.StartDeleteSizeGB}");
                    sb.AppendLine($"StopDeleteSizeGB={config.StopDeleteSizeGB}");
                    sb.AppendLine();
                }

                File.WriteAllText(ConfigFilePath, sb.ToString(), Encoding.UTF8);
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
        public static void AddOrUpdateConfig(string driveLetter, List<string> deleteDirectories, double startDeleteSizeGB, double stopDeleteSizeGB)
        {
            var configs = GetCurrentConfigs();
            var existingConfig = configs.FirstOrDefault(c => c.DriveLetter.Equals(driveLetter, StringComparison.OrdinalIgnoreCase));
            
            if (existingConfig != null)
            {
                existingConfig.DeleteDirectories = deleteDirectories;
                existingConfig.StartDeleteSizeGB = startDeleteSizeGB;
                existingConfig.StopDeleteSizeGB = stopDeleteSizeGB;
            }
            else
            {
                configs.Add(new DiskCleanupConfig
                {
                    DriveLetter = driveLetter,
                    DeleteDirectories = deleteDirectories,
                    StartDeleteSizeGB = startDeleteSizeGB,
                    StopDeleteSizeGB = stopDeleteSizeGB
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
    }
}