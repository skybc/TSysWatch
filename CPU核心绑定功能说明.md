# CPU 核心绑定功能实现

## 功能概述

为 TSysWatch CPU 核心管理器添加了具体的 CPU 核心绑定功能，允许用户为特定进程选择具体的 CPU 核心进行绑定，而不仅仅是设置核心数量。

## 核心特性

### 1. 多种绑定方式
- **即时绑定**：直接为运行中的进程设置核心绑定
- **进程名映射**：为特定进程名设置默认核心绑定
- **PID 映射**：为特定 PID 设置核心绑定（最高优先级）

### 2. 优先级系统
按照优先级从高到低：
1. **PID 核心绑定映射** - 最高优先级
2. **进程名核心绑定映射** - 高优先级
3. **PID 核心数映射** - 中等优先级
4. **进程名核心数映射** - 低优先级
5. **默认核心数** - 最低优先级

### 3. 智能核心选择
- **全选/全不选**：一键选择所有核心或清除选择
- **偶数核心**：选择 0, 2, 4, 6... 核心
- **奇数核心**：选择 1, 3, 5, 7... 核心
- **自定义选择**：手动选择任意核心组合

## 数据模型扩展

### ProcessCoreConfig 新增属性
```csharp
/// <summary>
/// 进程名称到具体核心绑定的映射
/// Key: 进程名, Value: 核心索引列表（如 "0,2,4"）
/// </summary>
public Dictionary<string, string> ProcessCoreBindingMapping { get; set; } = new();

/// <summary>
/// PID到具体核心绑定的映射（最高优先级）
/// Key: PID, Value: 核心索引列表（如 "0,2,4"）
/// </summary>
public Dictionary<int, string> PidCoreBindingMapping { get; set; } = new();
```

### ProcessInfo 新增属性
```csharp
/// <summary>
/// 配置的核心绑定（如果设置了具体核心绑定）
/// </summary>
public string? ConfiguredCoreBinding { get; set; }

/// <summary>
/// 当前绑定的核心列表
/// </summary>
public List<int> CurrentBoundCores { get; set; } = new();
```

### ProcessAffinityLog 新增属性
```csharp
/// <summary>
/// 设置类型（CoreCount 或 CoreBinding）
/// </summary>
public string SettingType { get; set; } = "CoreCount";

/// <summary>
/// 核心绑定详情（如果是核心绑定设置）
/// </summary>
public string? CoreBindingDetails { get; set; }
```

## 新增 API 接口

### 1. 设置进程核心绑定
```
POST /api/cpucore/process/{processId}/binding
Content-Type: application/json

{
  "processId": 1234,
  "coreIndices": [0, 2, 4]
}
```

### 2. 添加进程名核心绑定映射
```
POST /api/cpucore/mapping/process/binding
Content-Type: application/json

{
  "processName": "chrome",
  "coreIndices": [0, 2, 4]
}
```

### 3. 删除进程名核心绑定映射
```
DELETE /api/cpucore/mapping/process/binding/{processName}
```

### 4. 获取可用 CPU 核心
```
GET /api/cpucore/available-cores

Response:
{
  "success": true,
  "data": {
    "totalCores": 8,
    "cores": [
      { "index": 0, "name": "CPU 核心 0" },
      { "index": 1, "name": "CPU 核心 1" },
      ...
    ]
  }
}
```

## 用户界面改进

### 1. 进程列表表格
- 新增 **核心绑定** 列，显示配置的核心绑定和当前绑定状态
- 新增 **绑定核心** 按钮，提供图形化的核心选择界面

### 2. 核心绑定模态框
- **可视化核心选择**：复选框方式选择具体核心
- **快捷选择按钮**：全选、全不选、偶数核心、奇数核心
- **实时预览**：显示当前选择的核心组合

### 3. 配置管理
- **核心绑定映射管理**：在配置模态框中管理进程名的核心绑定
- **输入验证**：确保输入的核心索引有效

### 4. 操作日志
- **设置类型标识**：区分核心数量设置和核心绑定设置
- **详细绑定信息**：显示具体绑定的核心索引

## 配置文件格式

配置文件新增了两个节：

### [ProcessCoreBinding]
```ini
[ProcessCoreBinding]
# 格式: 进程名=核心索引列表(用逗号分隔) 例: chrome=0,2,4
# 核心绑定优先级高于核心数设置
chrome=0,2,4
firefox=1,3,5
```

### [PidCoreBinding]
```ini
[PidCoreBinding]
# 格式: PID=核心索引列表(用逗号分隔) 例: 1234=0,2,4
# PID 核心绑定优先级最高
1234=0,2,4
5678=1,3,5,7
```

## 使用场景示例

### 场景 1: 游戏性能优化
```
游戏进程绑定到性能核心: 0,2,4,6
后台程序绑定到效率核心: 1,3,5,7
```

### 场景 2: 视频渲染优化
```
视频编码器绑定到: 0,1,2,3
系统进程绑定到: 4,5,6,7
```

### 场景 3: 多任务负载均衡
```
浏览器绑定到: 0,2
音乐播放器绑定到: 1,3
开发工具绑定到: 4,5,6,7
```

## 技术实现亮点

### 1. 亲和性掩码计算
```csharp
private IntPtr CalculateAffinityMaskFromCores(List<int> coreIndices)
{
    long mask = 0;
    foreach (int coreIndex in coreIndices)
    {
        if (coreIndex >= 0 && coreIndex < Environment.ProcessorCount)
        {
            mask |= (1L << coreIndex);
        }
    }
    return new IntPtr(mask == 0 ? 1 : mask);
}
```

### 2. 核心绑定解析
```csharp
private List<int> ParseCoreBinding(string coreBinding)
{
    return coreBinding.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(s => int.TryParse(s.Trim(), out int core) ? core : -1)
        .Where(core => core >= 0 && core < Environment.ProcessorCount)
        .ToList();
}
```

### 3. 输入验证
- 核心索引范围验证（0 到 ProcessorCount-1）
- 重复核心索引去除
- 空值和无效输入处理

## 兼容性保证

- **向后兼容**：原有的核心数量设置功能完全保留
- **配置兼容**：旧的配置文件可以正常加载
- **API 兼容**：原有的 API 接口保持不变

## 性能优化

- **缓存机制**：配置信息缓存在内存中
- **批量处理**：扫描时批量应用配置
- **智能跳过**：相同掩码不重复设置
- **错误处理**：优雅处理无效进程和权限问题

## 安全考虑

- **权限检查**：所有操作都需要管理员权限
- **输入验证**：严格验证用户输入
- **错误日志**：详细记录所有操作和错误
- **系统保护**：跳过系统关键进程

这个实现为 CPU 核心管理提供了精细化的控制能力，让用户可以根据实际需求优化系统性能。