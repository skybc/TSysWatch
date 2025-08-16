using LibreHardwareMonitor.Hardware;
namespace TSysWatch;

/// <summary>
/// 更新访客：用于 LibreHardwareMonitor 的简单访客实现。
/// 该类会遍历计算机的硬件树并触发硬件更新，以刷新传感器数据。
/// </summary>
public class UpdateVisitor : IVisitor
{
    /// <summary>
    /// 访问计算机并使用此访客遍历其所有硬件节点。
    /// </summary>
    /// <param name="computer">要遍历和访问的计算机实例。</param>
    public void VisitComputer(IComputer computer)
    {
        // 调用 Traverse 来遍历计算机下的所有硬件节点，并在每个节点上调用本访客的方法
        computer.Traverse(this);
    }

    /// <summary>
    /// 访问并更新指定的硬件节点，然后对其子硬件继续递归遍历。
    /// </summary>
    /// <param name="hardware">要更新和遍历的硬件节点。</param>
    public void VisitHardware(IHardware hardware)
    {
        // 更新硬件的传感器数据（例如温度、风扇转速等）
        hardware.Update();

        // 递归访问子硬件
        foreach (var subHardware in hardware.SubHardware)
        {
            subHardware.Accept(this);
        }
    }

    /// <summary>
    /// 访问传感器（本实现不执行任何操作，仅为满足接口要求保留）。
    /// 如果需要，可在此处处理传感器读取值的逻辑。
    /// </summary>
    /// <param name="sensor">正在访问的传感器实例。</param>
    public void VisitSensor(ISensor sensor)
    {
        // 当前实现无需对单个传感器进行额外处理，保留空实现以兼容接口
    }

    /// <summary>
    /// 访问参数（本实现不执行任何操作，仅为满足接口要求保留）。
    /// </summary>
    /// <param name="parameter">正在访问的参数实例。</param>
    public void VisitParameter(IParameter parameter)
    {
        // 空实现：如果未来需要处理参数，可在此处添加逻辑
    }
}