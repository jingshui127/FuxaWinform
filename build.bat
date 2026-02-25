@echo off
echo Building FUXA Desktop (C# version)...
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed!
    echo Please install .NET 6.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET SDK found:
dotnet --version
echo.

REM Copy icon
copy "..\electrobun\icons\fuxa-logo.ico" . >nul

REM Build
echo Building...
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

if errorlevel 1 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo Output: bin\Release\net6.0-windows\win-x64\publish\FUXADesktop.exe
echo.
pause
