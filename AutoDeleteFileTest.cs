using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TSysWatch
{
    /// <summary>
    /// �Զ�ɾ���ļ����Թ���
    /// </summary>
    public static class AutoDeleteFileTest
    {
        /// <summary>
        /// �������ù���
        /// </summary>
        public static void TestConfiguration()
        {
            Console.WriteLine("=== �Զ�ɾ���ļ����ò��� ===");
            
            // ��ȡ��ǰ����
            var configs = AutoDeleteFileManager.GetCurrentConfigs();
            Console.WriteLine($"��ǰ����������{configs.Count}");
            
            foreach (var config in configs)
            {
                Console.WriteLine($"��������{config.DriveLetter}");
                Console.WriteLine($"ɾ��Ŀ¼��{string.Join(", ", config.DeleteDirectories)}");
                Console.WriteLine($"��ʼɾ����С��{config.StartDeleteSizeGB}GB");
                Console.WriteLine($"ֹͣɾ����С��{config.StopDeleteSizeGB}GB");
                Console.WriteLine();
            }
            
            // ��ȡ������Ϣ
            var drives = AutoDeleteFileManager.GetDriveInfos();
            Console.WriteLine("=== ������Ϣ ===");
            foreach (var drive in drives)
            {
                double totalSizeGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                double freeSizeGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                double usedSizeGB = totalSizeGB - freeSizeGB;
                
                Console.WriteLine($"��������{drive.Name}");
                Console.WriteLine($"�ܴ�С��{totalSizeGB:F2}GB");
                Console.WriteLine($"���ÿռ䣺{usedSizeGB:F2}GB");
                Console.WriteLine($"���ÿռ䣺{freeSizeGB:F2}GB");
                Console.WriteLine($"ʹ���ʣ�{(usedSizeGB / totalSizeGB * 100):F1}%");
                Console.WriteLine();
            }
            
            // ���Ŀ¼�Ƿ����
            if (configs.Any())
            {
                Console.WriteLine("=== Ŀ¼�����Լ�� ===");
                foreach (var config in configs)
                {
                    var checkResult = AutoDeleteFileManager.CheckDirectoriesExist(config.DeleteDirectories);
                    Console.WriteLine($"������ {config.DriveLetter} ��Ŀ¼��飺");
                    foreach (var kvp in checkResult)
                    {
                        Console.WriteLine($"  {kvp.Key}: {(kvp.Value ? "����" : "������")}");
                        if (kvp.Value)
                        {
                            long size = AutoDeleteFileManager.GetDirectorySize(kvp.Key);
                            Console.WriteLine($"    ��С: {AutoDeleteFileManager.FormatBytes(size)}");
                        }
                    }
                    Console.WriteLine();
                }
            }
        }
        
        /// <summary>
        /// ��Ӳ�������
        /// </summary>
        public static void AddTestConfiguration()
        {
            Console.WriteLine("=== ��Ӳ������� ===");
            
            // ���C������
            AutoDeleteFileManager.AddOrUpdateConfig("C:", 
                new List<string> { @"C:\temp", @"C:\Windows\temp", @"C:\Users\Public\temp" },
                5.0, 10.0);
            
            // ���D�����ã�������ڣ�
            if (Directory.Exists("D:\\"))
            {
                AutoDeleteFileManager.AddOrUpdateConfig("D:", 
                    new List<string> { @"D:\temp", @"D:\logs", @"D:\cache" },
                    10.0, 20.0);
            }
            
            Console.WriteLine("��������������");
        }
        
        /// <summary>
        /// ģ���������
        /// </summary>
        public static void SimulateCleanup()
        {
            Console.WriteLine("=== ģ��������� ===");
            
            var configs = AutoDeleteFileManager.GetCurrentConfigs();
            
            foreach (var config in configs)
            {
                try
                {
                    if (!Directory.Exists(config.DriveLetter))
                    {
                        Console.WriteLine($"�����������ڣ�{config.DriveLetter}");
                        continue;
                    }
                    
                    var driveInfo = new DriveInfo(config.DriveLetter);
                    if (!driveInfo.IsReady)
                    {
                        Console.WriteLine($"������δ׼���ã�{config.DriveLetter}");
                        continue;
                    }
                    
                    double freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    
                    Console.WriteLine($"������ {config.DriveLetter}:");
                    Console.WriteLine($"  ��ǰ���ÿռ�: {freeSpaceGB:F2}GB");
                    Console.WriteLine($"  ��ʼ������ֵ: {config.StartDeleteSizeGB}GB");
                    Console.WriteLine($"  ֹͣ������ֵ: {config.StopDeleteSizeGB}GB");
                    
                    if (freeSpaceGB <= config.StartDeleteSizeGB)
                    {
                        Console.WriteLine("  ��Ҫ����");
                        
                        // ��ȡ��ɾ�����ļ�
                        var filesToDelete = new List<FileInfo>();
                        foreach (var directory in config.DeleteDirectories)
                        {
                            if (Directory.Exists(directory))
                            {
                                var dirInfo = new DirectoryInfo(directory);
                                var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                                filesToDelete.AddRange(files);
                                Console.WriteLine($"    Ŀ¼ {directory}: {files.Length} ���ļ�");
                            }
                        }
                        
                        // ��ʱ������
                        var sortedFiles = filesToDelete.OrderBy(f => f.LastWriteTime).ToList();
                        Console.WriteLine($"  �ܹ��ҵ� {sortedFiles.Count} ����ɾ���ļ�");
                        
                        // ��ʾ��ɵļ����ļ�
                        Console.WriteLine("  ��ɵ��ļ�����������ɾ������");
                        foreach (var file in sortedFiles.Take(5))
                        {
                            Console.WriteLine($"    {file.FullName} - {file.LastWriteTime:yyyy-MM-dd HH:mm:ss} - {AutoDeleteFileManager.FormatBytes(file.Length)}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("  �ռ���㣬��������");
                    }
                    
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"���������� {config.DriveLetter} ʱ��������{ex.Message}");
                }
            }
        }
    }
}