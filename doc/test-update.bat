@REM Web 自更新系统 - 快速测试脚本
@REM 
@REM 使用说明:
@REM 1. 将此脚本放在有 update.zip 的目录
@REM 2. 修改 BASE_URL 为实际的 Web 地址
@REM 3. 运行脚本进行测试
@REM

@echo off
chcp 65001 >nul

setlocal enabledelayedexpansion

REM 配置
set "BASE_URL=http://localhost:5000"
set "UPDATE_FILE=update.zip"
set "LOG_FILE=update-test.log"

echo.
echo ======   自更新系统 - 快速测试 ======
echo.
echo 基础 URL: %BASE_URL%
echo 更新文件: %UPDATE_FILE%
echo.

REM 检查 update.zip 是否存在
if not exist "%UPDATE_FILE%" (
    echo [ERROR] 文件 %UPDATE_FILE% 不存在
    echo 请确保 %UPDATE_FILE% 在当前目录
    pause
    exit /b 1
)

echo [1/4] 检查健康状态...
curl -s "%BASE_URL%/api/self-update/health" > nul
if !errorlevel! neq 0 (
    echo [ERROR] Web 程序无法访问，请检查 URL
    echo 命令: curl -s "%BASE_URL%/api/self-update/health"
    pause
    exit /b 1
)
echo [✓] 系统正常运行
echo.

echo [2/4] 上传更新包...
echo 文件: %UPDATE_FILE%
curl -X POST "%BASE_URL%/api/self-update/upload" -F "file=@%UPDATE_FILE%" > "%LOG_FILE%"
echo 响应已保存到: %LOG_FILE%
echo.

type "%LOG_FILE%"
echo.

REM 检查上传是否成功
findstr /M "success.*true" "%LOG_FILE%" >nul
if !errorlevel! neq 0 (
    echo [ERROR] 上传失败，请检查错误日志
    pause
    exit /b 1
)
echo [✓] 上传成功
echo.

echo [3/4] 查询更新包信息...
curl -s "%BASE_URL%/api/self-update/package-info" > "%LOG_FILE%.info"
echo.
type "%LOG_FILE%.info"
echo.
echo [✓] 查询成功
echo.

echo [4/4] 触发更新...
echo.
echo ⚠️  警告: 即将触发 Web 程序更新，系统将暂时不可用
echo.
echo 按任意键继续，或 Ctrl+C 取消...
pause >nul
echo.

curl -X POST "%BASE_URL%/api/self-update/apply" > "%LOG_FILE%.apply"
echo.
type "%LOG_FILE%.apply"
echo.
echo [✓] 更新已触发
echo.
echo 请等待系统重启...
echo （通常需要 10-30 秒）
echo.

REM 等待 Web 重启
echo 等待 Web 恢复...
for /L %%i in (1,1,60) do (
    curl -s "%BASE_URL%/api/self-update/health" >nul
    if !errorlevel! equ 0 (
        echo.
        echo [✓] Web 已恢复正常
        echo 更新测试完成！
        pause
        exit /b 0
    )
    timeout /t 1 /nobreak
)

echo.
echo [×] Web 程序在规定时间内未恢复
echo 请手动检查系统状态
echo 查看 Updater.exe 日志: [Updater目录]\logs\updater_YYYYMMDD.txt
pause

exit /b 1
