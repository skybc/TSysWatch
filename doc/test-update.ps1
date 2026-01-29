# Web 自更新系统 - 快速测试脚本（PowerShell 版本）
#
# 使用说明:
# 1. 将此脚本放在有 update.zip 的目录
# 2. 修改 $BaseUrl 为实际的 Web 地址
# 3. 运行: powershell -ExecutionPolicy Bypass -File test-update.ps1

param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$UpdateFile = "update.zip",
    [string]$LogFile = "update-test.log"
)

function Write-Info {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Warning-Custom {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

# 检查 update.zip 是否存在
if (-not (Test-Path $UpdateFile)) {
    Write-Error-Custom "文件 $UpdateFile 不存在"
    Write-Host "请确保 $UpdateFile 在当前目录"
    exit 1
}

Write-Host "====== Web 自更新系统 - 快速测试 ======" -ForegroundColor Magenta
Write-Host ""
Write-Host "基础 URL  : $BaseUrl"
Write-Host "更新文件  : $UpdateFile"
Write-Host "文件大小  : $((Get-Item $UpdateFile).Length / 1MB) MB"
Write-Host ""

# 步骤 1: 检查健康状态
Write-Info "[1/4] 检查健康状态..."
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/self-update/health" -Method Get -ErrorAction Stop -TimeoutSec 5
    Write-Success "系统正常运行"
    Write-Host $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
}
catch {
    Write-Error-Custom "Web 程序无法访问，请检查 URL"
    Write-Host "错误: $_"
    exit 1
}

Write-Host ""

# 步骤 2: 上传更新包
Write-Info "[2/4] 上传更新包..."
try {
    $fileStream = [IO.File]::OpenRead($UpdateFile)
    $form = @{file=$fileStream}
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/self-update/upload" -Method Post -Form $form -TimeoutSec 120 -ErrorAction Stop
    
    $fileStream.Close()
    
    $result = $response.Content | ConvertFrom-Json
    
    if ($result.success) {
        Write-Success "上传成功"
        Write-Host "版本信息:"
        Write-Host $result | ConvertTo-Json -Depth 10
    }
    else {
        Write-Error-Custom "上传失败: $($result.message)"
        exit 1
    }
}
catch {
    Write-Error-Custom "上传出错: $_"
    exit 1
}

Write-Host ""

# 步骤 3: 查询更新包信息
Write-Info "[3/4] 查询更新包信息..."
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/self-update/package-info" -Method Get -TimeoutSec 10 -ErrorAction Stop
    $result = $response.Content | ConvertFrom-Json
    
    if ($result.success) {
        Write-Success "查询成功"
        Write-Host "当前更新包:"
        Write-Host $result.packageInfo | ConvertTo-Json -Depth 10
    }
}
catch {
    Write-Warning-Custom "查询包信息失败: $_"
}

Write-Host ""

# 步骤 4: 触发更新
Write-Info "[4/4] 触发更新..."
Write-Host ""
Write-Warning-Custom "警告: 即将触发 Web 程序更新，系统将暂时不可用"
Write-Host ""
$confirmation = Read-Host "是否继续？(Y/N)"

if ($confirmation -ne "Y" -and $confirmation -ne "y") {
    Write-Host "操作已取消"
    exit 0
}

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/self-update/apply" -Method Post -TimeoutSec 30 -ErrorAction Stop
    $result = $response.Content | ConvertFrom-Json
    
    if ($result.success) {
        Write-Success "更新已触发"
        Write-Host "可能还需要等待 2-5 秒，Web 程序正在停止..."
    }
    else {
        Write-Error-Custom "更新触发失败: $($result.message)"
        exit 1
    }
}
catch {
    Write-Info "Web 程序正在更新中（预期的 HTTP 连接中断）"
}

Write-Host ""
Write-Info "等待 Web 恢复..."

# 等待 Web 重启，最多等待 60 秒
$maxWait = 60
for ($i = 1; $i -le $maxWait; $i++) {
    Start-Sleep -Seconds 1
    
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/api/self-update/health" -Method Get -TimeoutSec 5 -ErrorAction Stop
        Write-Host ""
        Write-Success "Web 已恢复正常"
        Write-Host "更新测试完成！"
        Write-Host ""
        Write-Host "如需查看详细的更新日志，请查看:"
        Write-Host "  - Web 日志: [Web根目录]\logs\"
        Write-Host "  - Updater 日志: [Updater目录]\logs\updater_**.txt"
        exit 0
    }
    catch {
        # 继续等待
        if ($i % 10 -eq 0) {
            Write-Host "已等待 $i 秒..."
        }
    }
}

Write-Host ""
Write-Error-Custom "Web 程序在规定时间内未恢复"
Write-Host "请手动检查系统状态"
Write-Host "查看 Updater.exe 日志: [Updater目录]\logs\updater_YYYYMMDD.txt"
exit 1
