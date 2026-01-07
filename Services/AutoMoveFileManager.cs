using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TSysWatch
{
    /// <summary>
    /// 自动移动文件管理工具
    /// </summary>
    public static class AutoMoveFileManager
    {
        private static readonly string ConfigDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirPath, "AutoMoveFile.json");

        /// <summary>
        /// 从JSON文件获取当前配置
        /// </summary>
        /// <returns>配置列表</returns>
        public static List<AutoMoveConfig> GetCurrentConfigs()
        {
            var configs = new List<AutoMoveConfig>();

            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    LogHelper.Logger.Warning("配置文件不存在");
                    return configs;
                }

                var json = File.ReadAllText(ConfigFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                configs = JsonSerializer.Deserialize<List<AutoMoveConfig>>(json, options) ?? new List<AutoMoveConfig>();
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
        public static void SaveConfigs(List<AutoMoveConfig> configs)
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
        /// 添加或更新移动配置
        /// </summary>
        /// <param name="sourceDirectory">源目录</param>
        /// <param name="targetDrive">目标磁盘</param>
        /// <param name="moveTimeLimitMinutes">移动时间限制（分钟）</param>
        public static void AddOrUpdateConfig(string sourceDirectory, string targetDrive, int moveTimeLimitMinutes = 0)
        {
            var configs = GetCurrentConfigs();
            var existingConfig = configs.FirstOrDefault(c => c.SourceDirectory.Equals(sourceDirectory, StringComparison.OrdinalIgnoreCase));

            if (existingConfig != null)
            {
                existingConfig.TargetDrive = targetDrive;
                existingConfig.MoveTimeLimitMinutes = moveTimeLimitMinutes;
            }
            else
            {
                configs.Add(new AutoMoveConfig
                {
                    SourceDirectory = sourceDirectory,
                    TargetDrive = targetDrive,
                    MoveTimeLimitMinutes = moveTimeLimitMinutes
                });
            }

            SaveConfigs(configs);
        }

        /// <summary>
        /// 删除移动配置
        /// </summary>
        /// <param name="sourceDirectory">源目录</param>
        public static void RemoveConfig(string sourceDirectory)
        {
            var configs = GetCurrentConfigs();
            configs.RemoveAll(c => c.SourceDirectory.Equals(sourceDirectory, StringComparison.OrdinalIgnoreCase));
            SaveConfigs(configs);
        }

        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>目录是否存在</returns>
        public static bool CheckDirectoryExists(string directoryPath)
        {
            try
            {
                return Directory.Exists(directoryPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取目录中的文件数
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>文件数</returns>
        public static int GetFileCount(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return 0;

                return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories).Length;
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"获取目录文件数异常：{directoryPath}，错误：{ex.Message}");
                return 0;
            }
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
        /// 格式化字节大小为GB
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>GB大小</returns>
        public static double FormatBytesToGB(long bytes)
        {
            return Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 2);
        }
    }
}
