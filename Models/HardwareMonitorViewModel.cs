using System.Collections.Generic;

namespace TSysWatch
{
    /// <summary>
    /// 硬件监控页面视图模型
    /// </summary>
    public class HardwareMonitorViewModel
    {
        /// <summary>
        /// CPU 信息列表
        /// </summary>
        public List<HardwareGroupInfo> CpuInfos { get; set; } = new();

        /// <summary>
        /// GPU 信息列表
        /// </summary>
        public List<HardwareGroupInfo> GpuInfos { get; set; } = new();

        /// <summary>
        /// 内存信息
        /// </summary>
        public HardwareGroupInfo MemoryInfo { get; set; }

        /// <summary>
        /// 存储设备信息列表
        /// </summary>
        public List<HardwareGroupInfo> StorageInfos { get; set; } = new();

        /// <summary>
        /// 主板信息
        /// </summary>
        public List<HardwareGroupInfo> MotherboardInfos { get; set; } = new();

        /// <summary>
        /// 网络设备信息列表
        /// </summary>
        public List<HardwareGroupInfo> NetworkInfos { get; set; } = new();

        /// <summary>
        /// 电源信息
        /// </summary>
        public HardwareGroupInfo PowerSupplyInfo { get; set; }

        /// <summary>
        /// 电池信息
        /// </summary>
        public HardwareGroupInfo BatteryInfo { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 硬件组信息
    /// </summary>
    public class HardwareGroupInfo
    {
        /// <summary>
        /// 硬件名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 硬件类型
        /// </summary>
        public string HardwareType { get; set; } = "";

        /// <summary>
        /// 传感器信息列表
        /// </summary>
        public List<SensorDisplay> Sensors { get; set; } = new();

        /// <summary>
        /// 子硬件列表
        /// </summary>
        public List<HardwareGroupInfo> SubHardwares { get; set; } = new();
    }

    /// <summary>
    /// 传感器显示信息
    /// </summary>
    public class SensorDisplay
    {
        /// <summary>
        /// 传感器名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 传感器类型（Temperature, Voltage, Fan 等）
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

        /// <summary>
        /// 最小值
        /// </summary>
        public double? Min { get; set; }

        /// <summary>
        /// 最大值
        /// </summary>
        public double? Max { get; set; }

        /// <summary>
        /// 状态：Normal, Warning, Critical
        /// </summary>
        public string Status { get; set; } = "normal";

        /// <summary>
        /// 获取状态样式类
        /// </summary>
        public string GetStatusClass()
        {
            return Status.ToLower() switch
            {
                "critical" => "bg-danger text-white",
                "warning" => "bg-warning text-dark",
                _ => "bg-success text-white"
            };
        }

        /// <summary>
        /// 获取状态文本
        /// </summary>
        public string GetStatusText()
        {
            return Status.ToLower() switch
            {
                "critical" => "严重",
                "warning" => "警告",
                _ => "正常"
            };
        }
    }
}
