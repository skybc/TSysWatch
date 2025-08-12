using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TSysWatch
{
    /// <summary>
    /// �Զ�ɾ���ļ�������
    /// </summary>
    public static class AutoDeleteFileManager
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoDeleteFile.ini");

        /// <summary>
        /// ��ȡ��ǰ����
        /// </summary>
        /// <returns>�����б�</returns>
        public static List<DiskCleanupConfig> GetCurrentConfigs()
        {
            var configs = new List<DiskCleanupConfig>();

            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    LogHelper.Logger.Warning("�����ļ�������");
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

                if (currentConfig != null)
                {
                    configs.Add(currentConfig);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"��ȡ�����쳣��{ex.Message}", ex);
            }

            return configs;
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="configs">�����б�</param>
        public static void SaveConfigs(List<DiskCleanupConfig> configs)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# �Զ�ɾ���ļ�����");
                sb.AppendLine("# ��ʽ��[����������]");
                sb.AppendLine("# DeleteDirectories=Ŀ¼1,Ŀ¼2,Ŀ¼3");
                sb.AppendLine("# StartDeleteSizeGB=��ʼɾ��ʱ�Ĵ���ʣ��ռ�(GB)");
                sb.AppendLine("# StopDeleteSizeGB=ֹͣɾ��ʱ�Ĵ���ʣ��ռ�(GB)");
                sb.AppendLine("# StartDeleteFileDays=��ʼɾ���ļ�ʱ��(��) - ֻɾ������N����ļ���0��ʾ������ʱ��");
                sb.AppendLine("# LogicMode=ɾ�������߼���ϵ - AND(��)/OR(��)��AND��ʾͬʱ����������ʱ��������OR��ʾ������һ����");
                sb.AppendLine();

                foreach (var config in configs)
                {
                    sb.AppendLine($"[{config.DriveLetter}]");
                    sb.AppendLine($"DeleteDirectories={string.Join(",", config.DeleteDirectories)}");
                    sb.AppendLine($"StartDeleteSizeGB={config.StartDeleteSizeGB}");
                    sb.AppendLine($"StopDeleteSizeGB={config.StopDeleteSizeGB}");
                    sb.AppendLine($"StartDeleteFileDays={config.StartDeleteFileDays}");
                    sb.AppendLine($"LogicMode={config.LogicMode}");
                    sb.AppendLine();
                }

                File.WriteAllText(ConfigFilePath, sb.ToString(), Encoding.UTF8);
                LogHelper.Logger.Information("���ñ���ɹ�");
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"���������쳣��{ex.Message}", ex);
            }
        }

        /// <summary>
        /// ��ӻ���´�������
        /// </summary>
        /// <param name="driveLetter">��������ĸ</param>
        /// <param name="deleteDirectories">ɾ��Ŀ¼�б�</param>
        /// <param name="startDeleteSizeGB">��ʼɾ����С(GB)</param>
        /// <param name="stopDeleteSizeGB">ֹͣɾ����С(GB)</param>
        /// <param name="startDeleteFileDays">��ʼɾ���ļ�ʱ��(��)</param>
        /// <param name="logicMode">ɾ�������߼���ϵ</param>
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
        /// ɾ����������
        /// </summary>
        /// <param name="driveLetter">��������ĸ</param>
        public static void RemoveConfig(string driveLetter)
        {
            var configs = GetCurrentConfigs();
            configs.RemoveAll(c => c.DriveLetter.Equals(driveLetter, StringComparison.OrdinalIgnoreCase));
            SaveConfigs(configs);
        }

        /// <summary>
        /// ��ȡ������Ϣ
        /// </summary>
        /// <returns>������Ϣ�б�</returns>
        public static List<DriveInfo> GetDriveInfos()
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .ToList();
        }

        /// <summary>
        /// ���Ŀ¼�Ƿ����
        /// </summary>
        /// <param name="directories">Ŀ¼�б�</param>
        /// <returns>�����</returns>
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
        /// ��ȡĿ¼��С
        /// </summary>
        /// <param name="directoryPath">Ŀ¼·��</param>
        /// <returns>Ŀ¼��С���ֽڣ�</returns>
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
                LogHelper.Logger.Error($"��ȡĿ¼��С�쳣��{directoryPath}������{ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// ��ʽ���ֽڴ�С
        /// </summary>
        /// <param name="bytes">�ֽ���</param>
        /// <returns>��ʽ����Ĵ�С</returns>
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
        /// ����ļ��Ƿ�����ʱ���������ļ�����ָ��������
        /// </summary>
        /// <param name="filePath">�ļ�·��</param>
        /// <param name="days">������ֵ</param>
        /// <returns>�Ƿ�����ʱ������</returns>
        public static bool IsFileOlderThanDays(FileInfo fileInfo, int days)
        {
            if (days <= 0)
            {
                return true; // 0���ʾ������ʱ�䣬���Ƿ���true
            }

            try
            {

                // ȡ�ļ�������޸�ʱ��
                var fileTime = fileInfo.LastWriteTime;
                var threshold = DateTime.Now.AddDays(-days);
                return fileTime < threshold;
            }
            catch (Exception ex)
            {
                LogHelper.Logger.Error($"����ļ�ʱ���쳣��{fileInfo.FullName}������{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ����Ƿ�Ӧ��ɾ���ļ����������õ��߼���ϵ�жϣ�
        /// </summary>
        /// <param name="config">������������</param>
        /// <param name="currentFreeSpaceGB">��ǰ����ʣ��ռ�(GB)</param>
        /// <param name="filePath">�ļ�·��</param>
        /// <returns>�Ƿ�Ӧ��ɾ���ļ�</returns>
        public static DeleteReason ShouldDeleteFile(DiskCleanupConfig config, double currentFreeSpaceGB, FileInfo fileInfo)
        {
            // �����������
            bool capacityCondition = currentFreeSpaceGB < config.StartDeleteSizeGB;

            // ���ʱ������
            bool timeCondition = IsFileOlderThanDays(fileInfo, config.StartDeleteFileDays);

            // �����߼���ϵ���ؽ��
            switch (config.LogicMode)
            {
                case DeleteLogicMode.AND:
                    // �ң�����ͬʱ����������ʱ������
                    return new DeleteReason
                    {
                        CanDelete = capacityCondition && timeCondition,
                        Reason = "ͬʱ����������ʱ������",
                        FileInfo = fileInfo
                    };
                case DeleteLogicMode.OR:
                    // ������������ʱ������֮һ����
                    if (capacityCondition)
                    {
                        return new DeleteReason()
                        {
                            CanDelete = true,
                            Reason = "��������",
                            FileInfo = fileInfo
                        };
                    }
                    if (timeCondition)
                    {
                        return new DeleteReason()
                        {
                            CanDelete = true,
                            Reason = "�ļ�����",
                            FileInfo = fileInfo
                        };
                    }
                    else
                    {
                        return new DeleteReason()
                        {
                            CanDelete = false,
                            Reason = "������ɾ������",
                            FileInfo = fileInfo
                        };
                    }
                default:
                    if (capacityCondition)
                    {
                        return new DeleteReason()
                        {
                            CanDelete = true,
                            Reason = "��������",
                            FileInfo = fileInfo
                        };
                    }
                    if (timeCondition)
                    {
                        return new DeleteReason()
                        {
                            CanDelete = true,
                            Reason = "�ļ�����",
                            FileInfo = fileInfo
                        };
                    }
                    else
                    {
                        return new DeleteReason()
                        {
                            CanDelete = false,
                            Reason = "������ɾ������",
                            FileInfo = fileInfo
                        };
                    }
            }
        }
    }
}