# IntPtr JSON 序列化错误修复记录

## 问题描述
在调用 CPU 核心管理器的 API 接口时遇到以下 JSON 序列化错误：
```
NotSupportedException: Serialization and deserialization of 'System.IntPtr' instances is not supported.
Path: $.CurrentAffinityMask.
```

## 问题原因

### 根本原因
`ProcessInfo` 和 `ProcessAffinityLog` 模型中包含 `IntPtr` 类型的属性：

```csharp
public class ProcessInfo
{
    // 其他属性...
    public IntPtr CurrentAffinityMask { get; set; }  // 🚫 不支持 JSON 序列化
}

public class ProcessAffinityLog
{
    // 其他属性...
    public IntPtr OldAffinityMask { get; set; }      // 🚫 不支持 JSON 序列化
    public IntPtr NewAffinityMask { get; set; }      // 🚫 不支持 JSON 序列化
}
```

### 技术分析
- `System.IntPtr` 是非托管指针类型
- .NET 的 `System.Text.Json` 序列化器不支持 `IntPtr` 类型
- 当 API 返回包含 `IntPtr` 的对象时，序列化过程失败
- 错误发生在 `/api/cpucore/processes` 和 `/api/cpucore/logs` 端点

## 解决方案

### 1. 修改数据模型
将 `IntPtr` 类型改为 `string` 类型，存储十六进制格式的掩码：

```csharp
public class ProcessInfo
{
    // 其他属性...
    
    /// <summary>
    /// 当前亲和性掩码（十六进制字符串格式）
    /// </summary>
    public string CurrentAffinityMask { get; set; } = "0x0";  // ✅ 支持 JSON 序列化
}

public class ProcessAffinityLog
{
    // 其他属性...
    
    /// <summary>
    /// 旧亲和性掩码（十六进制字符串格式）
    /// </summary>
    public string OldAffinityMask { get; set; } = "0x0";      // ✅ 支持 JSON 序列化
    
    /// <summary>
    /// 新亲和性掩码（十六进制字符串格式）
    /// </summary>
    public string NewAffinityMask { get; set; } = "0x0";      // ✅ 支持 JSON 序列化
}
```

### 2. 修改数据转换逻辑
在 `CpuCoreManager.cs` 中更新相关方法：

#### LogOperation 方法
```csharp
private void LogOperation(int processId, string processName, IntPtr oldMask, 
    IntPtr newMask, bool success, string reason)
{
    var log = new ProcessAffinityLog
    {
        // 其他属性...
        OldAffinityMask = $"0x{oldMask.ToInt64():X}",  // 转换为十六进制字符串
        NewAffinityMask = $"0x{newMask.ToInt64():X}",  // 转换为十六进制字符串
        // 其他属性...
    };
}
```

#### GetCurrentProcesses 方法
```csharp
if (WindowsApi.GetProcessAffinityMask(processHandle, out IntPtr currentMask, out _))
{
    info.CurrentAffinityMask = $"0x{currentMask.ToInt64():X}";  // 转换为十六进制字符串
    info.CurrentCoreCount = CountBitsInMask(currentMask);
}
```

### 3. 更新前端代码
修改 JavaScript 中的显示逻辑：

```javascript
// 进程表格渲染
<td><code>${process.currentAffinityMask}</code></td>

// 日志表格渲染  
<td><code>${log.oldAffinityMask}</code></td>
<td><code>${log.newAffinityMask}</code></td>
```

## 修复效果

### ✅ 解决的问题
- **JSON 序列化错误完全消除**
- **API 接口正常工作** - 所有端点可以正常返回数据
- **数据完整性保持** - 亲和性掩码信息完全保留
- **显示效果一致** - 前端显示格式保持不变

### ✅ 保持的功能
- 进程列表 API 正常工作
- 操作日志 API 正常工作  
- Web 界面正常显示亲和性掩码
- 所有核心管理功能正常

### ✅ 改进的优势
- **更好的可读性** - 十六进制字符串更易理解
- **跨平台兼容** - 字符串格式更通用
- **调试友好** - 可以直接在 JSON 中看到掩码值
- **API 文档清晰** - 明确的数据格式

## 数据格式示例

### API 响应示例
```json
{
  "success": true,
  "data": [
    {
      "processId": 1234,
      "processName": "chrome",
      "configuredCoreCount": 2,
      "currentCoreCount": 2,
      "currentAffinityMask": "0x3",        // ✅ 字符串格式
      "isSystemCritical": false,
      "status": "Running"
    }
  ]
}
```

### 日志数据示例
```json
{
  "timestamp": "2024-01-15T10:30:00",
  "processId": 1234,
  "processName": "chrome",
  "oldAffinityMask": "0xF",               // ✅ 字符串格式
  "newAffinityMask": "0x3",               // ✅ 字符串格式
  "success": true,
  "reason": "设置成功"
}
```

## 技术最佳实践

### 经验教训
1. **避免在 API 模型中使用非托管类型** - `IntPtr`、`UIntPtr` 等
2. **选择合适的数据类型** - 优先使用可序列化的基本类型
3. **考虑跨平台兼容性** - 字符串比指针更通用
4. **提供有意义的默认值** - 避免 null 或未初始化状态

### 推荐做法
- 使用 `string` 存储十六进制值
- 使用 `long` 存储数值（如果需要计算）
- 在数据转换层处理类型转换
- 保持 API 接口的一致性

## 文件变更清单

### 修改文件
- `Services/Models/ProcessCoreConfig.cs` - 数据模型类型修改
- `Services/CpuCoreManager.cs` - 数据转换逻辑修改
- `Views/CpuCore/Index.cshtml` - 前端显示逻辑修改

### 测试验证
- ✅ 构建成功
- ✅ JSON 序列化正常
- ✅ API 接口可访问
- ✅ 数据格式正确

## 结论
通过将 `IntPtr` 类型改为 `string` 类型存储十六进制格式的掩码值，彻底解决了 JSON 序列化问题。这种方案不仅修复了当前错误，还提升了代码的可维护性、可读性和跨平台兼容性。

这是处理非托管类型 JSON 序列化问题的标准解决方案，适用于所有类似场景。