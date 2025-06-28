
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using LibreHardwareMonitor.Hardware;
using TSysWatch;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using System.Data;
using Terminal.Gui.Configuration;

internal class Program
{



    public class MonitorWindow : Window
    {
        private TableView processTable;
        private TableView hardwareTable;
        public MonitorWindow()
        {
            Title = "TSysWatch Monitor";
            processTable = new TableView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Percent(40) };
            Add(processTable);
            hardwareTable = new TableView { X = 0, Y = Pos.Bottom(processTable), Width = Dim.Fill(), Height = Dim.Fill() };
            Add(hardwareTable);
            
            Task.Run(RunMonitor);
        }

        public void RunMonitor()
        {
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
            // ========== Terminal.Gui 表格界面 ========== //
            // 只初始化一次
            bool guiStarted = false;

            System.Timers.Timer guiTimer = null;

            while (true)
            {
                try
                {
                    stringBuilder.Clear();

                    // 更新硬件信息,输出头信息
                    stringBuilder.AppendLine("Updating hardware information...");
                    // 打印进程信息                                
                    stringBuilder.AppendLine("-- Process Information --");
                    var processes = System.Diagnostics.Process.GetProcesses()
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
                    foreach (var process in processes)
                    {
                        stringBuilder.AppendLine($"{process.ProcessName,-50} {process.PID,-10} {process.MemoryUsage.ToString("F2"),-20} {process.PrivateMemorySize.ToString("F2"),-20} {process.HandleCount,-15} ");
                    }
                    stringBuilder.AppendLine("-- End of Process Information --");

                    stringBuilder.AppendLine("-- Hardware Information --");
                    stringBuilder.AppendLine("Hardware Information:");
                    var hardwareRows = new List<object[]>();
                    foreach (var hardware in _computer.Hardware)
                    {
                        stringBuilder.AppendLine($"Hardware: {hardware.Name}");
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
                            }
                        }
                        if (hardware.HardwareType == HardwareType.Motherboard)
                        {
                            foreach (var subHardware in hardware.SubHardware)
                            {
                                subHardware.Update();
                                stringBuilder.AppendLine($"Sub Hardware: {subHardware.Name}");
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
                                    }
                                }
                            }
                        }
                    }
                    string report = _computer.GetReport();
                    stringBuilder.AppendLine("-- End of Hardware Information --");
                    // Console.WriteLine(stringBuilder.ToString());
                    logger.Information(stringBuilder.ToString());

                    // ========== Terminal.Gui 2.x 表格刷新 ========== //

                    if (processTable != null && hardwareTable != null)
                    {
                        var processHeaders = new[] { "Process Name", "PID", "Memory Usage (MB)", "PeakWorkingSet64 (MB)", "Handle Count" };
                        var processRows = processes.Select(p => new object[]
                            { p.ProcessName, p.PID, p.MemoryUsage.ToString("F2"), p.PrivateMemorySize.ToString("F2"), p.HandleCount }).ToList();

                        Application.Invoke(() =>
                        {
                            // 更新表格数据
                            // 把processHeaders 和 processRows 转换为 DataTable
                            DataTable processDataTable = new DataTable();
                            foreach (var header in processHeaders)
                            {
                                processDataTable.Columns.Add(header);
                            }
                            foreach (var row in processRows)
                            {
                                processDataTable.Rows.Add(row);
                            }

                            processTable.Table = new DataTableSource(processDataTable); //(processRows, processHeaders);
                            var hardwareHeaders = new[] { "Hardware", "Sensor", "Value", "Unit" };
                            // 把hardwareRows 转换为 DataTable
                            DataTable hardwareDataTable = new DataTable();
                            foreach (var header in hardwareHeaders)
                            {
                                hardwareDataTable.Columns.Add(header);
                            }
                            foreach (var row in hardwareRows)
                            {
                                hardwareDataTable.Rows.Add(row);
                            }
                            hardwareTable.Table = new DataTableSource(hardwareDataTable);
                        });

                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "An error occurred while monitoring hardware.");
                }
                Thread.Sleep(5000); // 每5秒更新一次
            }
        }
    }
    private static void Main(string[] args)
    {
        // 读取exit退出 
        ConfigurationManager.RuntimeConfig = """{ "Theme": "Light" }""";
        ConfigurationManager.Enable(ConfigLocations.All);
        Application.Run<MonitorWindow>().Dispose();

        Application.Shutdown();

    }




}
