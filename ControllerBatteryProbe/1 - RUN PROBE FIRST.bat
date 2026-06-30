@echo off
cd /d "%~dp0"
title Battery Probe

where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET SDK not found.
    echo Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
    echo Then run this again.
    pause
    exit /b
)

echo Building and running the battery probe...
echo Power on your controllers / Bluetooth devices, then watch the output.
echo Press Ctrl+C to stop.
echo.
dotnet run -c Release
pause
