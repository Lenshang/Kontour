@echo off
chcp 65001 >nul
echo Starting application in debug mode...
dotnet build KontourApp.csproj -c Debug
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)

echo.
echo Starting application...
start "" "bin\Debug\net10.0-windows10.0.19041.0\win-x64\KontourApp.exe"
echo.
echo Application started.
echo Note: System.Diagnostics.Debug output can be viewed in Visual Studio Output window
echo.
pause
