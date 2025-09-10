# TSysWatch 管理员权限配置说明

## app.manifest 文件说明

TSysWatch 项目现在使用 `app.manifest` 文件来自动请求管理员权限，确保 CPU 核心管理器功能能够正常工作。

### 文件位置
```
TSysWatch/
├── app.manifest          # 应用程序清单文件
├── TSysWatch.csproj      # 项目文件（已配置引用清单）
└── ...
```

### 清单文件配置

app.manifest 文件包含以下关键配置：

```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

这个配置表示：
- **level="requireAdministrator"**: 程序启动时必须具有管理员权限
- **uiAccess="false"**: 不需要访问其他程序的用户界面

### 支持的 Windows 版本

清单文件明确声明支持以下 Windows 版本：
- Windows 7
- Windows 8
- Windows 8.1  
- Windows 10
- Windows 11

### 用户体验

当用户启动程序时：

1. **正常启动**：如果当前用户具有管理员权限，程序直接启动
2. **UAC 提示**：如果当前用户为普通用户，Windows 会显示 UAC（用户账户控制）对话框
3. **权限提升**：用户点击"是"后，程序以管理员权限启动
4. **拒绝权限**：用户点击"否"，程序无法启动

### 开发和部署注意事项

#### 开发环境
- Visual Studio 需要以管理员身份运行来调试程序
- 或者在项目属性中临时禁用清单文件进行开发调试

#### 生产环境
- 发布的程序会自动包含清单文件
- 用户首次运行时会看到 UAC 提示
- 建议在安装说明中提醒用户允许权限提升

### 权限验证

程序启动后会进行以下权限检查：

1. **管理员权限检查**
   ```csharp
   PrivilegeManager.IsRunningAsAdministrator()
   ```

2. **调试权限启用**
   ```csharp
   PrivilegeManager.EnableDebugPrivilege()
   ```

3. **功能可用性验证**
   - 如果权限不足，Web 界面会显示友好的错误信息
   - 提供权限问题的解决方案指导

### 故障排除

#### 问题：程序无法启动或权限不足

**解决方案：**
1. 确保以管理员身份运行程序
2. 检查 UAC 设置是否被禁用
3. 在企业环境中，联系 IT 管理员获取必要权限

#### 问题：开发时无法调试

**临时解决方案：**
```xml
<!-- 在开发时临时修改为 asInvoker -->
<requestedExecutionLevel level="asInvoker" uiAccess="false" />
```

**推荐解决方案：**
以管理员身份启动 Visual Studio

### 安全考虑

- 程序仅在需要时请求管理员权限
- 所有权限相关操作都有详细日志记录
- 遵循最小权限原则，仅访问必要的系统资源

### 相关文件

- `app.manifest` - 应用程序清单文件
- `Services/WindowsApi.cs` - Windows API 权限管理
- `Services/CpuCoreManagerService.cs` - 权限验证逻辑
- `Controllers/CpuCoreController.cs` - Web 权限检查
- `Views/CpuCore/Index.cshtml` - 权限错误显示

### 更多信息

有关 Windows 应用程序清单的更多信息，请参考：
- [Microsoft 文档：应用程序清单](https://docs.microsoft.com/en-us/windows/win32/sbscs/application-manifests)
- [UAC 最佳实践](https://docs.microsoft.com/en-us/windows/security/identity-protection/user-account-control/)