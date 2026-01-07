
using System.Text;
using LibreHardwareMonitor.Hardware;
using TSysWatch;

using System.Data;
using SqlSugar;
using TSysWatch.Entity;
using Tulip.Utils;
namespace TSysWatch.Services
{

    internal class MonitorWindow
    {

        public List<SensorInfo> SensorInfos { get; set; } = new List<SensorInfo>();
        public List<RealTimeData> RealTimeDatas { get; set; } = new List<RealTimeData>();
        /// <summary>
        /// 运行监控
        /// </summary>
        public void RunMonitor()
        {
            InitDb();
            // 启动监控
            var logger = LogHelper.Logger;
            logger.Information("TSysWatch started.");
            // 这里可以添加更多的监控逻辑
            // 例如，监控CPU、内存、磁盘等
            // 使用LibreHardwareMonitorLib库来获取硬件信息
            Computer _computer = new Computer()
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true,
                IsNetworkEnabled = true,
                IsStorageEnabled = true,
                IsPsuEnabled = true,
                IsBatteryEnabled = true,
            };
            _computer.Open();
            _computer.Accept(new UpdateVisitor());
            logger.Information("Hardware monitoring started.");
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                while (true)
                {
                    try
                    {
                        stringBuilder.Clear();
                        // 更新硬件信息,输出头信息
                        stringBuilder.AppendLine("Updating hardware information...");
                        // 打印进程信息                                
                        stringBuilder.AppendLine("-- Process Information --");
                        var processes = System.Diagnostics.Process.GetProcesses();
                        try
                        {
                            var topProcesses = processes
                                .OrderByDescending(p => p.WorkingSet64)
                                .Take(10)
                                .Select(p => new
                                {
                                    ProcessName = p.ProcessName,
                                    PID = p.Id,
                                    MemoryUsage = p.WorkingSet64 / 1024 / 1024,
                                    PrivateMemorySize = p.PeakWorkingSet64 / 1024 / 1024,
                                    HandleCount = p.HandleCount,
                                })
                                .ToList();
                            stringBuilder.AppendLine($"{"Process Name",-50} {"PID",-10} {"Memory Usage (MB)",-20} {"PeakWorkingSet64 (MB)",-20} {"Handle Count",-15} ");
                            foreach (var process in topProcesses)
                            {
                                stringBuilder.AppendLine($"{process.ProcessName,-50} {process.PID,-10} {process.MemoryUsage.ToString("F2"),-20} {process.PrivateMemorySize.ToString("F2"),-20} {process.HandleCount,-15} ");

                            }
                        }
                        finally
                        {
                            // 释放所有进程对象
                            foreach (var process in processes)
                            {
                                process?.Dispose();
                            }
                        }
                        stringBuilder.AppendLine("-- End of Process Information --");

                        stringBuilder.AppendLine("-- Hardware Information --");
                        stringBuilder.AppendLine("Hardware Information:");
                        var hardwareRows = new List<object[]>();
                        foreach (var hardware in _computer.Hardware)
                        {
                            stringBuilder.AppendLine($"Hardware: {hardware.Name},Hard type:{hardware.HardwareType}");
                            hardware.Update();
                            stringBuilder.AppendLine($"{"Sensor",-30} {"Value",-20} {"Unit",-10}");
                            foreach (var sensor in hardware.Sensors)
                            {
                                if (sensor.Value.HasValue)
                                {
                                    string unit = sensor.SensorType switch
                                    {
                                        SensorType.Temperature => "°C",
                                        SensorType.Voltage => "V",
                                        SensorType.Fan => "RPM",
                                        SensorType.Power => "W",
                                        SensorType.Load => "%",
                                        SensorType.Clock => "MHz",
                                        SensorType.Data => "Data",
                                        SensorType.Flow => "L/h",
                                        SensorType.Level => "%",
                                        SensorType.Frequency => "Hz",
                                        SensorType.Current => "A",
                                        SensorType.Energy => "J",
                                        SensorType.Control => "",
                                        SensorType.Factor => "",
                                        SensorType.SmallData => "",
                                        SensorType.Throughput => "",
                                        SensorType.TimeSpan => "",
                                        SensorType.Noise => "dB",
                                        SensorType.Conductivity => "",
                                        SensorType.Humidity => "",
                                        _ => ""
                                    };
                                    stringBuilder.AppendLine($"{sensor.Name,-30} {sensor.Value.Value.ToString("F2"),-20} {unit,-10}");
                                    hardwareRows.Add(new object[] { hardware.Name, sensor.Name, sensor.Value.Value.ToString("F2"), unit });
                                    // 添加到存储中（数据库）
                                }
                            }

                            foreach (var subHardware in hardware.SubHardware)
                            {
                                subHardware.Update();
                                stringBuilder.AppendLine($"Sub Hardware: {subHardware.Name},Hard type:{subHardware.HardwareType}");
                                stringBuilder.AppendLine($"{"Sensor",-30} {"Value",-20} {"Unit",-10}");
                                foreach (var subSensor in subHardware.Sensors)
                                {
                                    if (subSensor.Value.HasValue)
                                    {
                                        string subUnit = subSensor.SensorType switch
                                        {
                                            SensorType.Temperature => "°C",
                                            SensorType.Voltage => "V",
                                            SensorType.Fan => "RPM",
                                            SensorType.Power => "W",
                                            SensorType.Load => "%",
                                            SensorType.Clock => "MHz",
                                            SensorType.Data => "Data",
                                            SensorType.Flow => "L/h",
                                            SensorType.Level => "%",
                                            SensorType.Frequency => "Hz",
                                            SensorType.Current => "A",
                                            SensorType.Energy => "J",
                                            SensorType.Control => "",
                                            SensorType.Factor => "",
                                            SensorType.SmallData => "",
                                            SensorType.Throughput => "",
                                            SensorType.TimeSpan => "",
                                            SensorType.Noise => "dB",
                                            SensorType.Conductivity => "",
                                            SensorType.Humidity => "",
                                            _ => ""
                                        };
                                        stringBuilder.AppendLine($"{subSensor.Name,-30} {subSensor.Value.Value.ToString("F2"),-20} {subUnit,-10}");
                                        hardwareRows.Add(new object[] { $"{hardware.Name} - {subHardware.Name}", subSensor.Name, subSensor.Value.Value.ToString("F2"), subUnit });
                                        // 添加到存储中（数据库）
                                    }
                                }
                            }

                        }
                        // 清理 hardwareRows
                        hardwareRows.Clear();
                        string report = _computer.GetReport();
                        stringBuilder.AppendLine("-- End of Hardware Information --");
                        // 仅记录日志，不输出到控制台，减少I/O开销
                         logger.Information(stringBuilder.ToString());
                        Console.Clear();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "An error occurred while monitoring hardware.");
                    }
                    Thread.Sleep(5000); // 每5秒更新一次
                    GC.Collect();
                    // 清理控制台
                   

                }
            }
            finally
            {
                // 确保硬件资源被释放
                if (_computer != null)
                {
                    try
                    {
                        _computer.Close();
                    }
                    catch { }
                }
            }
        }

        private void InitDb()
        {
            try
            {
                // db
                using var db = CreateSqlSugarClient();
                db.CodeFirst.InitTables<RealTimeData>();
                db.CodeFirst.InitTables<SensorInfo>();
            }
            catch (Exception ex)
            {
            }
        }

        private ISqlSugarClient CreateSqlSugarClient()
        {

            // sqllite ,data目录下创建数据库文件，数据库文件名是HardwareData.db
            return new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = "Data Source=HardwareData.db",
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
                DbType = SqlSugar.DbType.Sqlite
            });
        }

        private DateTime lastDeleteTime = DateTime.MinValue;
 
    }




}
