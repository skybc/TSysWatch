using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TSysWatch
{
    /// <summary>
    /// 自动删除文件测试工具
    /// </summary>
    public static class AutoDeleteFileTest
    {
        /// <summary>
        /// 测试配置功能
        /// </summary>
        public static void TestConfiguration()
        {
            Console.WriteLine("=== 自动删除文件配置测试 ===");
            
            // 获取当前配置
            var configs = AutoDeleteFileManager.GetCurrentConfigs();
            Console.WriteLine($"当前配置数量：{configs.Count}");
            
            foreach (var config in configs)
            {
                Console.WriteLine($"驱动器：{config.DriveLetter}");
                Console.WriteLine($"删除目录：{string.Join(", ", config.DeleteDirectories)}");
                Console.WriteLine($"开始删除大小：{config.StartDeleteSizeGB}GB");
                Console.WriteLine($"停止删除大小：{config.StopDeleteSizeGB}GB");
                Console.WriteLine();
            }
            
            // 获取磁盘信息
            var drives = AutoDeleteFileManager.GetDriveInfos();
            Console.WriteLine("=== 磁盘信息 ===");
            foreach (var drive in drives)
            {
                double totalSizeGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                double freeSizeGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                double usedSizeGB = totalSizeGB - freeSizeGB;
                
                Console.WriteLine($"驱动器：{drive.Name}");
                Console.WriteLine($"总大小：{totalSizeGB:F2}GB");
                Console.WriteLine($"已用空间：{usedSizeGB:F2}GB");
                Console.WriteLine($"可用空间：{freeSizeGB:F2}GB");
                Console.WriteLine($"使用率：{(usedSizeGB / totalSizeGB * 100):F1}%");
                Console.WriteLine();
            }
            
            // 检查目录是否存在
            if (configs.Any())
            {
                Console.WriteLine("=== 目录存在性检查 ===");
                foreach (var config in configs)
                {
                    var checkResult = AutoDeleteFileManager.CheckDirectoriesExist(config.DeleteDirectories);
                    Console.WriteLine($"驱动器 {config.DriveLetter} 的目录检查：");
                    foreach (var kvp in checkResult)
                    {
                        Console.WriteLine($"  {kvp.Key}: {(kvp.Value ? "存在" : "不存在")}");
                        if (kvp.Value)
                        {
                            long size = AutoDeleteFileManager.GetDirectorySize(kvp.Key);
                            Console.WriteLine($"    大小: {AutoDeleteFileManager.FormatBytes(size)}");
                        }
                    }
                    Console.WriteLine();
                }
            }
        }
        
        /// <summary>
        /// 添加测试配置
        /// </summary>
        public static void AddTestConfiguration()
        {
            Console.WriteLine("=== 添加测试配置 ===");
            
            // 添加C盘配置
            AutoDeleteFileManager.AddOrUpdateConfig("C:", 
                new List<string> { @"C:\temp", @"C:\Windows\temp", @"C:\Users\Public\temp" },
                5.0, 10.0);
            
            // 添加D盘配置（如果存在）
            if (Directory.Exists("D:\\"))
            {
                AutoDeleteFileManager.AddOrUpdateConfig("D:", 
                    new List<string> { @"D:\temp", @"D:\logs", @"D:\cache" },
                    10.0, 20.0);
            }
            
            Console.WriteLine("测试配置添加完成");
        }
        
        /// <summary>
        /// 模拟清理过程
        /// </summary>
        public static void SimulateCleanup()
        {
            Console.WriteLine("=== 模拟清理过程 ===");
            
            var configs = AutoDeleteFileManager.GetCurrentConfigs();
            
            foreach (var config in configs)
            {
                try
                {
                    if (!Directory.Exists(config.DriveLetter))
                    {
                        Console.WriteLine($"驱动器不存在：{config.DriveLetter}");
                        continue;
                    }
                    
                    var driveInfo = new DriveInfo(config.DriveLetter);
                    if (!driveInfo.IsReady)
                    {
                        Console.WriteLine($"驱动器未准备好：{config.DriveLetter}");
                        continue;
                    }
                    
                    double freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    
                    Console.WriteLine($"驱动器 {config.DriveLetter}:");
                    Console.WriteLine($"  当前可用空间: {freeSpaceGB:F2}GB");
                    Console.WriteLine($"  开始清理阈值: {config.StartDeleteSizeGB}GB");
                    Console.WriteLine($"  停止清理阈值: {config.StopDeleteSizeGB}GB");
                    
                    if (freeSpaceGB <= config.StartDeleteSizeGB)
                    {
                        Console.WriteLine("  需要清理！");
                        
                        // 获取可删除的文件
                        var filesToDelete = new List<FileInfo>();
                        foreach (var directory in config.DeleteDirectories)
                        {
                            if (Directory.Exists(directory))
                            {
                                var dirInfo = new DirectoryInfo(directory);
                                var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                                filesToDelete.AddRange(files);
                                Console.WriteLine($"    目录 {directory}: {files.Length} 个文件");
                            }
                        }
                        
                        // 按时间排序
                        var sortedFiles = filesToDelete.OrderBy(f => f.LastWriteTime).ToList();
                        Console.WriteLine($"  总共找到 {sortedFiles.Count} 个可删除文件");
                        
                        // 显示最旧的几个文件
                        Console.WriteLine("  最旧的文件（将被优先删除）：");
                        foreach (var file in sortedFiles.Take(5))
                        {
                            Console.WriteLine($"    {file.FullName} - {file.LastWriteTime:yyyy-MM-dd HH:mm:ss} - {AutoDeleteFileManager.FormatBytes(file.Length)}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("  空间充足，无需清理");
                    }
                    
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理驱动器 {config.DriveLetter} 时发生错误：{ex.Message}");
                }
            }
        }
    }
}