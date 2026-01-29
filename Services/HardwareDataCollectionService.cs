using LibreHardwareMonitor.Hardware;
using System.Text;
using TSysWatch.Models;

namespace TSysWatch.Services
{
    /// <summary>
    /// 硬件数据采集和CSV存储服务
    /// </summary>
    public class HardwareDataCollectionService
    {
        private readonly HardwareMonitorConfigManager _configManager;
        private readonly ILogger<HardwareDataCollectionService> _logger;
        private static Computer? _computer;

        public HardwareDataCollectionService(
            HardwareMonitorConfigManager configManager,
            ILogger<HardwareDataCollectionService> logger)
        {
            _configManager = configManager;
            _logger = logger;
            InitializeComputer();
        }

        /// <summary>
        /// 初始化硬件监控
        /// </summary>
        private static void InitializeComputer()
        {
            if (_computer != null) return;

            _computer = new Computer()
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true,
                IsNetworkEnabled = true,
                IsStorageEnabled = true,
                IsBatteryEnabled = true,
            };

            _computer?.Open();
            _computer?.Accept(new UpdateVisitor());
        }

        /// <summary>
        /// 收集当前硬件数据
        /// </summary>
        public List<HardwareSensorData> CollectHardwareData()
        {
            var result = new List<HardwareSensorData>();
            var config = _configManager.GetConfig();

            try
            {
                _computer?.Accept(new UpdateVisitor());

                foreach (var hardware in _computer?.Hardware ?? new IHardware[0])
                {
                    hardware.Update();
                    CollectHardwareSensors(hardware, config, result);

                    // 处理子硬件
                    if (hardware.SubHardware != null)
                    {
                        foreach (var subHardware in hardware.SubHardware)
                        {
                            subHardware.Update();
                            CollectHardwareSensors(subHardware, config, result, hardware.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"收集硬件数据失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 收集单个硬件的传感器数据
        /// </summary>
        private void CollectHardwareSensors(
            IHardware hardware,
            HardwareMonitorConfig config,
            List<HardwareSensorData> result,
            string parentName = "")
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (!sensor.Value.HasValue) continue;

                var sensorType = sensor.SensorType.ToString();
                
                // 如果配置了特定的传感器类型，只记录这些类型
                if (config.RecordedSensorTypes.Count > 0 &&
                    !config.RecordedSensorTypes.Contains(sensorType))
                {
                    continue;
                }

                result.Add(new HardwareSensorData
                {
                    Timestamp = DateTime.Now,
                    HardwareName = string.IsNullOrEmpty(parentName) ? hardware.Name : $"{parentName} - {hardware.Name}",
                    HardwareType = hardware.HardwareType.ToString(),
                    SensorName = sensor.Name,
                    SensorType = sensorType,
                    Value = sensor.Value.Value,
                    Unit = GetSensorUnit(sensor.SensorType)
                });
            }
        }

        /// <summary>
        /// 获取传感器单位
        /// </summary>
        private string GetSensorUnit(SensorType sensorType)
        {
            return sensorType switch
            {
                SensorType.Temperature => "°C",
                SensorType.Voltage => "V",
                SensorType.Fan => "RPM",
                SensorType.Power => "W",
                SensorType.Load => "%",
                SensorType.Clock => "MHz",
                SensorType.Data => "GB",
                SensorType.Flow => "L/h",
                SensorType.Level => "%",
                SensorType.Frequency => "Hz",
                SensorType.Current => "A",
                SensorType.Energy => "J",
                SensorType.SmallData => "MB",
                SensorType.Throughput => "Mbps",
                SensorType.TimeSpan => "h",
                SensorType.Noise => "dB",
                SensorType.Humidity => "%",
                _ => ""
            };
        }

        /// <summary>
        /// 保存数据到CSV文件
        /// </summary>
        public bool SaveToCsv(List<HardwareSensorData> data)
        {
            if (data == null || data.Count == 0) return false;

            try
            {
                var config = _configManager.GetConfig();
                var csvPath = config.GetFullCsvPath();

                if (!Directory.Exists(csvPath))
                {
                    Directory.CreateDirectory(csvPath);
                }

                // 按日期分类存储
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var fileName = $"HardwareData_{today}.csv";
                var filePath = Path.Combine(csvPath, fileName);

                // 生成新的列头
                var newColumnHeaders = GenerateColumnHeaders(data);
                var newSensorColumns = newColumnHeaders.Skip(1).ToList(); // 除去Timestamp列

                bool shouldRewriteFile = false;
                List<string> existingColumnHeaders = null;

                // 检查文件是否存在
                if (File.Exists(filePath))
                {
                    // 读取现有文件的列头
                    var firstLine = File.ReadLines(filePath).FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstLine))
                    {
                        existingColumnHeaders = ParseCsvLine(firstLine);
                        
                        // 比较列头是否相同
                        if (!AreColumnsEqual(existingColumnHeaders, newColumnHeaders))
                        {
                            shouldRewriteFile = true;
                            _logger.LogInformation($"CSV列头变化，需要重写文件: {filePath}");
                        }
                    }
                }

                if (shouldRewriteFile && existingColumnHeaders != null)
                {
                    // 读取整个现有文件，除去列头
                    var existingLines = File.ReadAllLines(filePath).Skip(1).ToList();
                    
                    // 重新写入文件：新列头 + 现有数据行
                    var csv = new StringBuilder();
                    csv.AppendLine(string.Join(",", newColumnHeaders.Select(EscapeCsvValue)));
                    
                    // 添加现有数据行
                    foreach (var line in existingLines)
                    {
                        csv.AppendLine(line);
                    }

                    // 添加新数据行
                    var dataByTimestamp = data.GroupBy(d => d.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                    foreach (var group in dataByTimestamp)
                    {
                        var row = BuildCsvRow(group.Key, group.ToList(), newSensorColumns);
                        csv.AppendLine(string.Join(",", row.Select(EscapeCsvValue)));
                    }

                    // 覆盖写入整个文件
                    File.WriteAllText(filePath, csv.ToString());
                    _logger.LogInformation($"CSV文件已重写（列头变化）: {filePath}");
                }
                else
                {
                    // 新文件或列头未变，正常追加
                    var csv = new StringBuilder();

                    // 如果是新文件，写入列头
                    if (!File.Exists(filePath))
                    {
                        csv.AppendLine(string.Join(",", newColumnHeaders.Select(EscapeCsvValue)));
                    }

                    // 写入数据行
                    var dataByTimestamp = data.GroupBy(d => d.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                    foreach (var group in dataByTimestamp)
                    {
                        var row = BuildCsvRow(group.Key, group.ToList(), newSensorColumns);
                        csv.AppendLine(string.Join(",", row.Select(EscapeCsvValue)));
                    }

                    File.AppendAllText(filePath, csv.ToString());
                    _logger.LogInformation($"硬件数据已保存到: {filePath}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"保存CSV文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 生成列头列表
        /// </summary>
        private List<string> GenerateColumnHeaders(List<HardwareSensorData> data)
        {
            var columnHeaders = new List<string> { "Timestamp" };
            var sensorColumns = new List<string>();

            // 生成唯一的列名，处理重复的SensorName-SensorType
            var columnMap = new Dictionary<(string SensorName, string SensorType), List<string>>();
            
            foreach (var item in data)
            {
                var key = (item.SensorName, item.SensorType);
                if (!columnMap.ContainsKey(key))
                {
                    columnMap[key] = new List<string>();
                }
                if (!columnMap[key].Contains(item.HardwareName))
                {
                    columnMap[key].Add(item.HardwareName);
                }
            }

            // 生成列名
            foreach (var kvp in columnMap)
            {
                var (sensorName, sensorType) = kvp.Key;
                var hardwareNames = kvp.Value;
                
                var columnName = $"{sensorName}-{sensorType}";
                if (hardwareNames.Count > 1)
                {
                    // 有重复，需要为每个硬件单独添加列
                    foreach (var hwName in hardwareNames)
                    {
                        sensorColumns.Add($"{columnName}({hwName})");
                    }
                }
                else
                {
                    // 没有重复，直接添加
                    sensorColumns.Add(columnName);
                }
            }

            columnHeaders.AddRange(sensorColumns);
            return columnHeaders;
        }

        /// <summary>
        /// 构建CSV数据行
        /// </summary>
        private List<string> BuildCsvRow(string timestamp, List<HardwareSensorData> groupData, List<string> sensorColumns)
        {
            var row = new List<string> { timestamp };
            
            foreach (var column in sensorColumns)
            {
                var value = "";
                
                // 解析列名
                if (column.Contains("(") && column.Contains(")"))
                {
                    // 格式：SensorName-SensorType(HardwareName)
                    var match = System.Text.RegularExpressions.Regex.Match(
                        column,
                        @"^(.+?)-(.+?)\((.+?)\)$");
                    
                    if (match.Success)
                    {
                        var sensorName = match.Groups[1].Value;
                        var sensorType = match.Groups[2].Value;
                        var hwName = match.Groups[3].Value;
                        
                        var item = groupData.FirstOrDefault(d =>
                            d.SensorName == sensorName &&
                            d.SensorType == sensorType &&
                            d.HardwareName == hwName);
                        
                        if (item != null)
                        {
                            value = item.Value.ToString("F2");
                        }
                    }
                }
                else
                {
                    // 格式：SensorName-SensorType
                    var parts = column.Split("-");
                    if (parts.Length == 2)
                    {
                        var sensorName = parts[0];
                        var sensorType = parts[1];
                        
                        var item = groupData.FirstOrDefault(d =>
                            d.SensorName == sensorName &&
                            d.SensorType == sensorType);
                        
                        if (item != null)
                        {
                            value = item.Value.ToString("F2");
                        }
                    }
                }
                
                row.Add(value);
            }
            
            return row;
        }

        /// <summary>
        /// 解析CSV行（简单的CSV解析，不处理复杂情况）
        /// </summary>
        private List<string> ParseCsvLine(string line)
        {
            var columns = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    columns.Add(current.ToString().Trim('"'));
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                columns.Add(current.ToString().Trim('"'));
            }

            return columns;
        }

        /// <summary>
        /// 比较两个列头是否相等
        /// </summary>
        private bool AreColumnsEqual(List<string> columns1, List<string> columns2)
        {
            if (columns1.Count != columns2.Count)
                return false;

            for (int i = 0; i < columns1.Count; i++)
            {
                if (columns1[i] != columns2[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 转义CSV值（处理包含逗号和引号的值）
        /// </summary>
        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        /// <summary>
        /// 获取CSV文件列表（按日期范围）
        /// </summary>
        public List<string> GetCsvFiles(DateTime? startDate = null, DateTime? endDate = null)
        {
            var config = _configManager.GetConfig();
            var csvPath = config.GetFullCsvPath();

            if (!Directory.Exists(csvPath))
            {
                return new List<string>();
            }

            var files = Directory.GetFiles(csvPath, "HardwareData_*.csv")
                .OrderByDescending(f => f)
                .ToList();

            // 如果指定了日期范围，进行过滤
            if (startDate.HasValue || endDate.HasValue)
            {
                files = files.Where(f =>
                {
                    var fileName = Path.GetFileName(f);
                    if (!DateTime.TryParseExact(
                        fileName.Replace("HardwareData_", "").Replace(".csv", ""),
                        "yyyy-MM-dd",
                        null,
                        System.Globalization.DateTimeStyles.None,
                        out var fileDate))
                    {
                        return false;
                    }

                    if (startDate.HasValue && fileDate < startDate.Value)
                        return false;

                    if (endDate.HasValue && fileDate > endDate.Value)
                        return false;

                    return true;
                }).ToList();
            }

            return files;
        }
    }

    /// <summary>
    /// 硬件传感器数据模型
    /// </summary>
    public class HardwareSensorData
    {
        /// <summary>
        /// 采集时间
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 硬件名称
        /// </summary>
        public string HardwareName { get; set; } = "";
        
        /// <summary>
        /// 硬件类型
        /// </summary>
        public string HardwareType { get; set; } = "";
        
        /// <summary>
        /// 传感器名称
        /// </summary>
        public string SensorName { get; set; } = "";
        
        /// <summary>
        /// 传感器类型
        /// </summary>
        public string SensorType { get; set; } = "";
        
        /// <summary>
        /// 传感器值
        /// </summary>
        public double Value { get; set; }
        
        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; } = "";
    }
}
