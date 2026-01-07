# AutoMoveFile 和 AutoCopyFile 管理页面使用指南

## 快速开始

### AutoMoveFile（自动移动文件）

**访问地址**: http://localhost:端口/AutoMoveFile/Index

#### 使用步骤

1. **添加移动任务**
   - 填写源目录（要移动的文件所在目录）
   - 填写目标目录（移动到的目标目录）
   - 可选：设置移动时间限制（分钟）
   - 点击"添加/更新配置"

2. **配置说明**
   - 移动时间限制 = 0：无限制，所有文件都会被移动
   - 移动时间限制 > 0：只移动最后修改时间超过N分钟的文件
   - 示例：设置为 60 则只移动1小时前修改过的文件

3. **管理配置**
   - 编辑：修改源目录对应的配置
   - 删除：删除不需要的移动任务
   - 刷新：实时更新配置列表

### AutoCopyFile（自动拷贝文件）

**访问地址**: http://localhost:端口/AutoCopyFile/Index

#### 使用步骤

1. **添加拷贝任务**
   - 填写源目录（要拷贝的文件所在目录）
   - 填写目标目录（拷贝到的目标目录）
   - 可选：设置已拷贝文件目录（拷贝完成后的处理）
   - 点击"添加/更新配置"

2. **配置说明**
   - 已拷贝文件目录配置：
     - 已设置：拷贝完成后将源文件移动到该目录
     - 未设置：拷贝完成后删除源文件
   - 支持的图片格式：jpg, jpeg, png, bmp, gif, tiff, tif, webp, ico

3. **管理配置**
   - 编辑：修改源目录对应的配置
   - 删除：删除不需要的拷贝任务
   - 刷新：实时更新配置列表

### AutoDeleteFile（自动删除文件）

**访问地址**: http://localhost:端口/AutoDelete/Index

该页面提供了完整的删除文件管理功能，包括删除条件测试工具。

## 配置文件说明

### AutoMoveFile.ini

```ini
[MoveTask]
SourceDirectory=D:\Downloads
TargetDirectory=E:\Archive
MoveTimeLimitMinutes=0

[MoveTask]
SourceDirectory=C:\Temp
TargetDirectory=D:\Temp_Backup
MoveTimeLimitMinutes=60
```

### AutoCopyFile.ini

```ini
[CopyTask]
SourceDirectory=D:\Pictures
TargetDirectory=E:\Backup\Pictures
MovedDirectory=D:\Pictures_Processed

[CopyTask]
SourceDirectory=C:\Documents
TargetDirectory=E:\Backup\Documents
```

## 常见问题

### Q: 为什么无法添加配置？
A: 检查以下几点：
- 源目录必须存在（当前验证源目录存在性）
- 目标目录如果不存在会自动创建
- 确保应用有权限读写这些目录

### Q: 配置保存后为什么没有立即生效？
A: 页面显示会自动刷新，但后台服务的任务执行需要等待下一次轮询周期（通常10-60秒）

### Q: 支持多个任务吗？
A: 支持！可以添加多个移动/拷贝任务，系统会依次处理

### Q: 如何删除配置？
A: 在配置列表中找到要删除的任务，点击"删除"按钮，确认删除即可

## 系统架构

```
Web界面（Index.cshtml）
     ↓
JavaScript Fetch
     ↓
Controller API
  (GetConfigs, SaveConfig, DeleteConfig)
     ↓
Manager 业务逻辑
  (AutoMoveFileManager, AutoCopyFileManager)
     ↓
INI文件读写
  (使用ini-parser库)
     ↓
AutoMoveFile, AutoCopyFile 后台服务
```

## 更新日志

- 2026-01-07: 创建AutoMoveFile和AutoCopyFile管理页面
- 2026-01-07: 创建对应的Manager类和API接口
- 2026-01-07: 集成到导航菜单

---

**注意**: 所有配置更改需要管理员权限。确保运行应用的用户有适当的文件系统权限。
