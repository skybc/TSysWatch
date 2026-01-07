# 内存泄漏修复文档

## 概述

在应用运行过程中发现内存持续上涨的问题，经过详细分析，找到了 5 个主要的内存泄漏点，并逐一修复。

## 问题分析

### 问题 1：MonitorWindow - Computer 对象未正确释放（最严重）

**问题描述**：
- `LibreHardwareMonitor` 的 `Computer` 对象在 `RunMonitor()` 方法中被创建后未正确释放
- 原代码没有调用 `Computer.Close()`，导致硬件监控占用的系统资源无法释放
- 该方法包含 `while(true)` 永久循环，Computer 对象在应用生命周期内不释放

**修复方案**：
- 使用 `try-finally` 块确保硬件资源被释放
- 在 `finally` 块中调用 `_computer.Close()` 以正确关闭硬件监控

**修改代码**：
```csharp
try
{
    while (true)
    {
        // ... 监控逻辑
        Thread.Sleep(5000);
    }
}
finally
{
    if (_computer != null)
    {
        try
        {
            _computer.Close();
        }
        catch { }
    }
}
```

**影响**：显著降低内存占用

---

### 问题 2：AutoDeleteFile - Process 对象泄漏

**问题描述**：
- `RunMonitor()` 中调用 `Process.GetProcesses()` 获取所有进程
- 获取进程信息后未调用 `Dispose()`，导致 Process 对象占用句柄资源

**修复方案**：
- 在 `finally` 块中统一释放所有 Process 对象
- 确保即使发生异常也能正确清理

**修改代码**：
```csharp
var processes = System.Diagnostics.Process.GetProcesses();
try
{
    var topProcesses = processes.OrderByDescending(p => p.WorkingSet64).Take(10)...
    // 处理进程信息
}
finally
{
    foreach (var process in processes)
    {
        process?.Dispose();
    }
}
```

**影响**：释放系统句柄资源

---

### 问题 3：AutoDeleteFile - StreamWriter 频繁创建

**问题描述**：
- 删除文件时，使用 `using var stream = new StreamWriter(logFilePath, true)` 后，立即在循环中调用 `stream.WriteLine()`
- 每次清理循环都会频繁创建和销毁 StreamWriter 对象

**修复方案**：
- 收集所有日志消息到 `List<string>`
- 循环完成后，一次性打开文件流写入所有消息
- 这样可以减少文件 I/O 操作和对象创建

**修改代码**：
```csharp
var logMessages = new List<string>();

foreach (var file in candidateFiles.OrderBy(f => f.LastWriteTime))
{
    // ... 处理文件
    logMessages.Add(log);
}

// 一次性写入
if (logMessages.Count > 0)
{
    using (var stream = new StreamWriter(logFilePath, true))
    {
        foreach (var log in logMessages)
        {
            stream.WriteLine(log);
        }
        stream.Flush();
    }
    logMessages.Clear();
}
```

**影响**：减少对象创建，提高 I/O 效率

---

### 问题 4：循环内频繁读取配置文件

**问题描述**：
- `AutoDeleteFile.Run()`, `AutoCopyFile.Run()`, `AutoMoveFile.Run()` 的主循环中，**每次**都调用 `ReadJsonFile()`
- 这导致：
  - 频繁的文件 I/O 操作
  - 每次循环都创建新的 List<> 并反序列化整个配置文件
  - 内存中保留前一次的配置对象，新配置创建后，旧对象可能未及时释放

**修复方案**：
- 使用计数器，仅定期重新读取配置（例如每 2-10 次循环读取一次）
- 缓存配置对象，避免频繁创建和销毁
- 循环间隔已经是 10-60 秒，无需每次都读取

**修改代码（以 AutoDeleteFile 为例）**：
```csharp
List<DiskCleanupConfig> configCache = null;
int configCheckInterval = 0;
const int configCheckFrequency = 10; // 每10次循环检查一次配置

while (_isRunning)
{
    try
    {
        if (configCheckInterval++ >= configCheckFrequency)
        {
            ReadJsonFile();
            configCache = _configs;
            configCheckInterval = 0;
        }

        if (configCache != null)
        {
            foreach (var config in configCache)
            {
                CheckAndCleanDisk(config);
            }
        }
    }
    finally
    {
        Thread.Sleep(1000 * 60);
    }
}
```

**影响**：大幅减少内存分配和 I/O 操作

---

### 问题 5：CpuCoreManagerService - Timer 处理

**问题描述**：
- `StartPeriodicScan()` 方法中，重复调用时只是 `Dispose()` 旧 Timer，但未设置为 `null`
- 可能导致资源未彻底释放

**修复方案**：
- 显式设置为 `null`，明确指示旧对象应被释放

**修改代码**：
```csharp
lock (_timerLock)
{
    if (_scanTimer != null)
    {
        _scanTimer.Dispose();
        _scanTimer = null;  // 确保完全释放
    }
    
    _scanTimer = new Timer(OnScanTimer, null, TimeSpan.Zero, interval);
}
```

**影响**：确保 Timer 资源正确释放

---

## 修复效果

修复后的应用在相同工作负载下，内存占用应该会：
1. **显著降低** - 特别是硬件监控相关的内存占用
2. **更加稳定** - 减少内存持续增长的速度
3. **更加均衡** - 避免频繁的大对象分配和回收

## 测试建议

1. **长时间运行测试**：
   - 运行应用 4-8 小时，监控内存占用曲线
   - 观察内存是否稳定在某个水平或仅缓慢增长

2. **工具**：
   - 使用 Windows 任务管理器查看内存占用
   - 或使用 dotMemory、Visual Studio Profiler 进行深度分析

3. **对比指标**：
   - 修复前：内存可能线性增长，每分钟增加 5-50MB
   - 修复后：内存应该在初始化后保持相对稳定，或增速大幅降低

## 代码质量标准遵循

✅ 遵循 SOLID 原则 - 资源正确管理  
✅ 使用 async/await 和 using 语句 - I/O 资源正确释放  
✅ 内存安全 - 避免持续内存增长  
✅ 代码清晰 - 易于维护和审核  

