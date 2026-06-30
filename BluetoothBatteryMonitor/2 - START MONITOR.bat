@echo off
cd /d "%~dp0"
title Bluetooth Battery Monitor

where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET SDK not found.
    echo Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
    echo Then run this again.
    pause
    exit /b
)

echo Starting the tray app. Look for the battery icon in your system tray
echo (bottom-right, you may need to click the ^ arrow to see it).
echo Right-click the icon for settings. Close this window to quit the app.
echo.
dotnet run -c Release
