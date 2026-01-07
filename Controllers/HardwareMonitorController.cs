using LibreHardwareMonitor.Hardware;
using Microsoft.AspNetCore.Mvc;
using TSysWatch.Models;
using System;
using System.Collections.Generic;

namespace TSysWatch.Controllers
{
    /// <summary>
    /// 硬件监控控制器
    /// </summary>
    public class HardwareMonitorController : Controller
    {
        private static Computer _computer;

        /// <summary>
        /// 初始化硬件监控
        /// </summary>
        static HardwareMonitorController()
        {
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

            _computer.Open();
            _computer.Accept(new UpdateVisitor());
        }

        /// <summary>
        /// 硬件监控首页
        /// </summary>
        public IActionResult Index()
        {
            var viewModel = GetHardwareMonitorData();
            return View(viewModel);
        }

        /// <summary>
        /// 获取硬件监控内容（HTML片段）
        /// </summary>
        [HttpGet]
        public IActionResult GetHardwareContent()
        {
            try
            {
                var viewModel = GetHardwareMonitorData();
                return PartialView("_HardwareContent", viewModel);
            }
            catch (Exception ex)
            {
                return Content($"<div class='error'>加载失败: {ex.Message}</div>");
            }
        }

        /// <summary>
        /// 获取硬件监控数据API
        /// </summary>
        [HttpGet]
        public IActionResult GetHardwareData()
        {
            try
            {
                var viewModel = GetHardwareMonitorData();
                return Json(new
                {
                    success = true,
                    data = viewModel,
                    lastUpdateTime = viewModel.LastUpdateTime
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取硬件监控数据
        /// </summary>
        private HardwareMonitorViewModel GetHardwareMonitorData()
        {
            var viewModel = new HardwareMonitorViewModel();

            // 更新所有硬件信息
            _computer.Accept(new UpdateVisitor());

            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();

                switch (hardware.HardwareType)
                {
                    case HardwareType.Cpu:
                        viewModel.CpuInfos.Add(ConvertHardwareToGroupInfo(hardware));
                        break;

                    case HardwareType.GpuAmd:
                    case HardwareType.GpuIntel:
                    case HardwareType.GpuNvidia:
                        viewModel.GpuInfos.Add(ConvertHardwareToGroupInfo(hardware));
                        break;

                    case HardwareType.Memory:
                        viewModel.MemoryInfo = ConvertHardwareToGroupInfo(hardware);
                        break;

                    case HardwareType.Motherboard:
                        viewModel.MotherboardInfos.Add(ConvertHardwareToGroupInfo(hardware));
                        break;

                    case HardwareType.Network:
                        viewModel.NetworkInfos.Add(ConvertHardwareToGroupInfo(hardware));
                        break;

                    case HardwareType.Battery:
                        viewModel.BatteryInfo = ConvertHardwareToGroupInfo(hardware);
                        break;

                    default:
                        // Storage 和其他硬件类型
                        if (hardware.HardwareType.ToString().Contains("Storage") || 
                            hardware.HardwareType.ToString().Contains("HDD") ||
                            hardware.HardwareType.ToString().Contains("SSD"))
                        {
                            viewModel.StorageInfos.Add(ConvertHardwareToGroupInfo(hardware));
                        }
                        break;
                }

                // 处理子硬件
                if (hardware.SubHardware != null && hardware.SubHardware.Length > 0)
                {
                    var group = viewModel.GetGroupByType(hardware.HardwareType);
                    if (group != null)
                    {
                        foreach (var sub in hardware.SubHardware)
                        {
                            sub.Update();
                            var subInfo = ConvertHardwareToGroupInfo(sub);
                            subInfo.Name = $"{hardware.Name} - {sub.Name}";
                            group.SubHardwares.Add(subInfo);
                        }
                    }
                }
            }

            viewModel.LastUpdateTime = DateTime.Now;
            return viewModel;
        }

        /// <summary>
        /// 将硬件对象转换为硬件组信息
        /// </summary>
        private HardwareGroupInfo ConvertHardwareToGroupInfo(IHardware hardware)
        {
            var groupInfo = new HardwareGroupInfo
            {
                Name = hardware.Name,
                HardwareType = hardware.HardwareType.ToString()
            };

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.Value.HasValue)
                {
                    var sensorDisplay = ConvertSensorToDisplay(sensor);
                    groupInfo.Sensors.Add(sensorDisplay);
                }
            }

            return groupInfo;
        }

        /// <summary>
        /// 将传感器对象转换为传感器显示信息
        /// </summary>
        private SensorDisplay ConvertSensorToDisplay(ISensor sensor)
        {
            var unit = GetSensorUnit(sensor.SensorType);
            var status = GetSensorStatus(sensor.SensorType, sensor.Value ?? 0);

            return new SensorDisplay
            {
                Name = sensor.Name,
                SensorType = sensor.SensorType.ToString(),
                Value = sensor.Value ?? 0,
                Unit = unit,
                Min = sensor.Min,
                Max = sensor.Max,
                Status = status
            };
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
                SensorType.Control => "",
                SensorType.Factor => "",
                SensorType.SmallData => "MB",
                SensorType.Throughput => "Mbps",
                SensorType.TimeSpan => "h",
                SensorType.Noise => "dB",
                SensorType.Conductivity => "S/m",
                SensorType.Humidity => "%",
                _ => ""
            };
        }

        /// <summary>
        /// 根据传感器类型和值获取状态
        /// </summary>
        private string GetSensorStatus(SensorType sensorType, double value)
        {
            return sensorType switch
            {
                // CPU 温度：> 95°C 为严重，> 80°C 为警告
                SensorType.Temperature when value > 95 => "critical",
                SensorType.Temperature when value > 80 => "warning",

                // CPU 负载：> 90% 为警告
                SensorType.Load when value > 90 => "warning",

                // 风扇转速：< 1000 RPM 为警告
                SensorType.Fan when value < 1000 && value > 0 => "warning",

                // 电压：偏离 ±10% 为警告
                SensorType.Voltage when (value < 11.7 || value > 14.3) => "warning",

                // 内存占用：> 90% 为警告
                SensorType.Data when value > 90 => "warning",

                _ => "normal"
            };
        }
    }

    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class HardwareMonitorExtensions
    {
        /// <summary>
        /// 根据硬件类型获取对应的硬件组
        /// </summary>
        public static HardwareGroupInfo GetGroupByType(this HardwareMonitorViewModel viewModel, HardwareType hardwareType)
        {
            return hardwareType switch
            {
                HardwareType.Memory => viewModel.MemoryInfo,
                HardwareType.Battery => viewModel.BatteryInfo,
                _ => null
            };
        }
    }
}
