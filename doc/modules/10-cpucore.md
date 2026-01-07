# CPU 核心绑定功能（CpuCore）

## 概述

CPU 核心绑定功能允许用户为特定进程分配 CPU 核心，提高关键应用的性能并防止 CPU 资源争抢。

## 功能特性

- **进程绑定**: 为应用进程绑定特定的 CPU 核心
- **动态管理**: 支持运行时查看和修改绑定配置
- **冲突预防**: 防止多个进程占用同一核心
- **性能优化**: 减少上下文切换，提高 CPU 缓存命中率

## 技术架构

### 核心组件
- **CpuCoreManager.cpp**: 本地 C++ 代码，处理底层 Windows API 调用
- **CpuCoreManagerService.cs**: .NET 包装类，提供托管接口
- **CpuCoreManagerServiceWrapper.cs**: 服务包装类，进行 P/Invoke 调用

### 平台依赖
- Windows 系统 API（SetProcessAffinityMask）
- 本地 DLL（CpuCoreManager.dll）

## 配置管理

### 位置
`ini_config/CpuCore.json`

### 格式
```json
[
  {
    "ProcessName": "notepad.exe",
    "CoreIndex": 0
  }
]
```

## 相关文件

| 文件 | 用途 |
|-----|-----|
| Controllers/CpuCoreController.cs | 控制器逻辑 |
| Services/CpuCoreManager.cs | 业务逻辑 |
| Services/CpuCoreManagerService.cs | 服务实现 |
| Services/CpuCoreManagerServiceWrapper.cs | 包装器 |
| CpuCoreManager.cpp | 本地代码 |
| Models/CpuCoreIndexViewModel.cs | 视图模型 |
| Views/CpuCore/Index.cshtml | 主视图 |

## 后续改进

- [ ] 支持多核绑定
- [ ] 支持进程优先级设置
- [ ] 支持 NUMA 优化
- [ ] 支持绑定规则持久化

## 相关文档

- [CPU核心绑定功能说明](../../CPU核心绑定功能说明.md)
- [进程名核心绑定完整实现指南](../../进程名核心绑定完整实现指南.md)
