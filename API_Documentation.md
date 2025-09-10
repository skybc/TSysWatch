# CPU 核心数管理器 API 文档

## API 基础地址
```
http://localhost:5000/api/cpucore
```

## 认证
所有 API 均需要程序以管理员权限运行。

## API 端点

### 1. 获取系统配置
**GET** `/config`

**响应示例：**
```json
{
    "success": true,
    "data": {
        "defaultCoreCount": 4,
        "scanIntervalSeconds": 2,
        "enabled": true,
        "processNameMapping": {
            "chrome": 2,
            "firefox": 2,
            "notepad": 1
        },
        "pidMapping": {
            "1234": 8
        },
        "criticalProcesses": ["System", "csrss", "wininit", "winlogon"]
    }
}
```

### 2. 更新系统配置
**POST** `/config`

**请求体：**
```json
{
    "defaultCoreCount": 4,
    "scanIntervalSeconds": 2,
    "enabled": true,
    "processNameMapping": {
        "chrome": 2,
        "firefox": 2
    },
    "pidMapping": {},
    "criticalProcesses": ["System", "csrss"]
}
```

**响应示例：**
```json
{
    "success": true,
    "message": "配置更新成功"
}
```

### 3. 获取进程列表
**GET** `/processes`

**响应示例：**
```json
{
    "success": true,
    "data": [
        {
            "processId": 1234,
            "processName": "chrome",
            "configuredCoreCount": 2,
            "currentCoreCount": 4,
            "currentAffinityMask": "0x0F",
            "lastUpdated": "2024-01-15T10:30:00",
            "isSystemCritical": false,
            "status": "Running"
        }
    ]
}
```

### 4. 设置进程核心数
**POST** `/process/{processId}/cores/{coreCount}`

**参数：**
- processId: 进程 ID
- coreCount: 核心数量

**响应示例：**
```json
{
    "success": true,
    "message": "设置成功"
}
```

### 5. 添加进程名映射
**POST** `/mapping/process`

**请求体：**
```json
{
    "processName": "chrome",
    "coreCount": 2
}
```

**响应示例：**
```json
{
    "success": true,
    "message": "映射添加成功"
}
```

### 6. 删除进程名映射
**DELETE** `/mapping/process/{processName}`

**响应示例：**
```json
{
    "success": true,
    "message": "映射删除成功"
}
```

### 7. 手动触发扫描
**POST** `/scan`

**响应示例：**
```json
{
    "success": true,
    "message": "扫描已触发"
}
```

### 8. 获取操作日志
**GET** `/logs?count=100`

**参数：**
- count: 返回日志条数（默认100）

**响应示例：**
```json
{
    "success": true,
    "data": [
        {
            "timestamp": "2024-01-15T10:30:00",
            "processId": 1234,
            "processName": "chrome",
            "oldAffinityMask": "0x0F",
            "newAffinityMask": "0x03",
            "success": true,
            "reason": "设置成功"
        }
    ]
}
```

### 9. 获取系统信息
**GET** `/system`

**响应示例：**
```json
{
    "success": true,
    "data": {
        "processorCount": 8,
        "isAdministrator": true,
        "configFilePath": "C:\\App\\CpuCoreManager.ini",
        "machineName": "DESKTOP-ABC123",
        "osVersion": "Microsoft Windows NT 10.0.19045.0",
        "workingSet": 52428800,
        "tickCount": 123456789
    }
}
```

## 错误响应格式
```json
{
    "success": false,
    "message": "错误描述"
}
```

## PowerShell 测试脚本示例

```powershell
# 设置基础 URL
$baseUrl = "http://localhost:5000/api/cpucore"

# 获取系统配置
Invoke-RestMethod -Uri "$baseUrl/config" -Method GET

# 获取进程列表
Invoke-RestMethod -Uri "$baseUrl/processes" -Method GET

# 设置进程核心数
Invoke-RestMethod -Uri "$baseUrl/process/1234/cores/2" -Method POST

# 添加进程名映射
$body = @{
    processName = "chrome"
    coreCount = 2
} | ConvertTo-Json

Invoke-RestMethod -Uri "$baseUrl/mapping/process" -Method POST -Body $body -ContentType "application/json"

# 手动触发扫描
Invoke-RestMethod -Uri "$baseUrl/scan" -Method POST

# 获取操作日志
Invoke-RestMethod -Uri "$baseUrl/logs?count=50" -Method GET
```