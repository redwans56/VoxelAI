@echo off
setlocal
echo ===================================================
echo   Building VoxelAI (Native Windows x86/x64)
echo ===================================================

set CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC_PATH%" (
    set CSC_PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
)

if not exist "%CSC_PATH%" (
    echo [ERROR] Microsoft .NET Framework 4.0/4.8 csc.exe compiler not found!
    if not defined CI pause
    exit /b 1
)

if not exist "bin\Release" mkdir "bin\Release"

echo Compiling C# source files with optimizations and unsafe code support...
"%CSC_PATH%" /nologo /target:winexe /optimize+ /unsafe+ /platform:x86 /out:bin\Release\VoxelAI.exe /r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Xml.dll /recurse:src\*.cs

if %ERRORLEVEL% equ 0 (
    echo.
    echo [SUCCESS] Build succeeded! Binary produced at bin\Release\VoxelAI.exe
    copy /Y bin\Release\VoxelAI.exe VoxelAI.exe >nul
    echo Copied to root as VoxelAI.exe
) else (
    echo.
    echo [ERROR] Compilation failed.
    exit /b %ERRORLEVEL%
)
