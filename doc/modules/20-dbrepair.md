# 数据库维修功能（DbRepair）

## 概述

数据库维修功能提供对系统数据库的诊断和修复能力，确保数据库的完整性和性能。

## 功能特性

- **数据库诊断**: 检查数据库文件完整性
- **自动修复**: 修复损坏的数据库结构
- **备份保护**: 修复前自动创建备份
- **日志记录**: 记录修复过程和结果

## 技术架构

### 核心组件
- **DbRepair.cs**: 数据库维修逻辑实现

### 数据库类型
- SQLite（主要使用）

## 配置管理

### 位置
`ini_config/DbRepair.json`

### 格式
```json
{
  "DatabasePath": "HardwareData.db",
  "BackupPath": "Backups",
  "AutoRepair": true,
  "LogPath": "Logs"
}
```

## 相关文件

| 文件 | 用途 |
|-----|-----|
| Services/DbRepair.cs | 维修逻辑 |
| Controllers/HomeController.cs | 控制器 |

## 后续改进

- [ ] 支持 SQL Server 数据库
- [ ] 支持数据库性能分析
- [ ] 支持自动碎片整理
- [ ] 支持备份还原功能

## 相关文档

- [DbRepair_README.md](../../DbRepair_README.md)
