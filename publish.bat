@echo off
chcp 65001 >nul
echo ========================================
echo    Kontour 应用一键打包发布脚本
echo ========================================
echo.

:: 设置变量
set PROJECT_NAME=KontourApp
set PROJECT_FILE=KontourApp.csproj
set TARGET_FRAMEWORK=net10.0-windows10.0.19041.0
set RUNTIME=win-x64
set OUTPUT_DIR=.\publish\%RUNTIME%

echo [检测] 正在检测可用的 .NET SDK 版本...
dotnet --list-sdks
echo.

:: 检查是否安装了 .NET 10
dotnet --list-sdks | findstr /C:"10." >nul
if %ERRORLEVEL% neq 0 (
    echo ⚠ 警告: 未检测到 .NET 10 SDK
    echo.
    echo 将尝试使用框架依赖发布模式...
    echo 注意: 目标机器需要安装 .NET 10 运行时
    echo.
    set SELF_CONTAINED=false
) else (
    echo ✓ 检测到 .NET 10 SDK
    set SELF_CONTAINED=true
)
echo.

echo [1/4] 准备发布环境...

:: 检查是否需要临时修改项目文件
echo 正在检查项目配置...
findstr /C:"net10.0-android" "%PROJECT_FILE%" >nul
if %ERRORLEVEL% equ 0 (
    echo 检测到多平台配置，创建临时发布配置...
    
    :: 备份原始项目文件
    copy "%PROJECT_FILE%" "%PROJECT_FILE%.backup" >nul
    echo ✓ 已备份项目文件
    
    :: 创建只包含Windows平台的临时配置
    powershell -Command "(Get-Content '%PROJECT_FILE%') -replace '<TargetFrameworks>.*</TargetFrameworks>', '<TargetFrameworks>%TARGET_FRAMEWORK%</TargetFrameworks>' | Set-Content '%PROJECT_FILE%'"
    echo ✓ 已创建临时发布配置（仅Windows平台）
    set NEED_RESTORE=true
) else (
    echo ✓ 项目配置正常
    set NEED_RESTORE=false
)
echo.

echo [2/4] 清理旧的发布文件...
if exist "%OUTPUT_DIR%" (
    rmdir /s /q "%OUTPUT_DIR%"
    echo ✓ 已清理旧文件
) else (
    echo ✓ 无需清理
)
echo.

echo [3/4] 开始编译和发布...
echo 目标框架: %TARGET_FRAMEWORK%
echo 运行时: %RUNTIME%
echo 输出目录: %OUTPUT_DIR%
echo 自包含模式: %SELF_CONTAINED%
echo.

if "%SELF_CONTAINED%"=="true" (
    echo 使用自包含发布模式（包含运行时）...
    :: 发布为单文件应用程序
    dotnet publish "%PROJECT_FILE%" ^
        -c Release ^
        -f %TARGET_FRAMEWORK% ^
        -r %RUNTIME% ^
        --self-contained true ^
        -p:PublishSingleFile=true ^
        -p:IncludeNativeLibrariesForSelfExtract=true ^
        -p:PublishTrimmed=false ^
        -p:EnableCompressionInSingleFile=true ^
        -p:RuntimeIdentifier=%RUNTIME% ^
        /p:TargetFrameworks=%TARGET_FRAMEWORK% ^
        -o "%OUTPUT_DIR%"
) else (
    echo 使用框架依赖发布模式（需要目标机器安装 .NET 运行时）...
    :: 框架依赖模式
    dotnet publish "%PROJECT_FILE%" ^
        -c Release ^
        -f %TARGET_FRAMEWORK% ^
        -r %RUNTIME% ^
        --self-contained false ^
        -p:PublishSingleFile=true ^
        -p:RuntimeIdentifier=%RUNTIME% ^
        /p:TargetFrameworks=%TARGET_FRAMEWORK% ^
        -o "%OUTPUT_DIR%"
)

if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ 发布失败！
    
    :: 恢复项目文件
    if "%NEED_RESTORE%"=="true" (
        echo 正在恢复项目文件...
        move /Y "%PROJECT_FILE%.backup" "%PROJECT_FILE%" >nul
        echo ✓ 已恢复项目文件
    )
    
    pause
    exit /b 1
)

echo.
echo ✓ 发布成功！
echo.

:: 恢复项目文件
if "%NEED_RESTORE%"=="true" (
    echo 正在恢复项目文件...
    move /Y "%PROJECT_FILE%.backup" "%PROJECT_FILE%" >nul
    echo ✓ 已恢复项目文件
    echo.
)

echo [4/4] 检查生成的文件...
if exist "%OUTPUT_DIR%\%PROJECT_NAME%.exe" (
    echo ✓ 找到可执行文件: %PROJECT_NAME%.exe
    
    :: 获取文件大小
    for %%A in ("%OUTPUT_DIR%\%PROJECT_NAME%.exe") do (
        set SIZE=%%~zA
        set /a SIZE_MB=%%~zA/1024/1024
    )
    echo   文件大小: !SIZE_MB! MB
) else (
    echo ❌ 未找到可执行文件！
    
    :: 恢复项目文件
    if "%NEED_RESTORE%"=="true" (
        echo 正在恢复项目文件...
        move /Y "%PROJECT_FILE%.backup" "%PROJECT_FILE%" >nul
    )
    
    pause
    exit /b 1
)
echo.

echo [5/5] 打包完成！
echo.
echo ========================================
echo 发布位置: %CD%\%OUTPUT_DIR%
echo 可执行文件: %PROJECT_NAME%.exe
echo ========================================
echo.
echo 提示：
if "%SELF_CONTAINED%"=="true" (
    echo 1. 可以直接运行 %PROJECT_NAME%.exe
    echo 2. 这是一个独立的可执行文件，包含了所有必需的依赖
    echo 3. 可以复制到任何 Windows 10/11 x64 电脑上运行
) else (
    echo 1. 可以直接运行 %PROJECT_NAME%.exe
    echo 2. ⚠ 目标电脑需要安装 .NET 10 运行时才能运行
    echo 3. 下载地址: https://dotnet.microsoft.com/download/dotnet/10.0
    echo 4. 或者安装 .NET 10 SDK 后重新运行此脚本生成自包含版本
)
echo.

:: 询问是否打开发布目录
set /p OPEN_FOLDER="是否打开发布目录? (Y/N): "
if /i "%OPEN_FOLDER%"=="Y" (
    explorer "%OUTPUT_DIR%"
)

echo.
echo 按任意键退出...
pause >nul
