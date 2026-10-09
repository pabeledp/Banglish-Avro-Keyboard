@echo off
echo ========================================================
echo Building Banglish for Windows (Native .NET Executable)
echo ========================================================

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo Error: C# Compiler (csc.exe) not found at %CSC%
    exit /b 1
)

echo Compiling C# source files...
"%CSC%" /nologo /target:winexe /out:Banglish.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll src\AvroData.cs src\BanglishEngine.cs src\KeyboardHook.cs src\CandidateForm.cs src\TrayApp.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================================
    echo SUCCESS! Banglish.exe has been compiled successfully.
    echo Output: windows\Banglish.exe
    echo ========================================================
) else (
    echo.
    echo ========================================================
    echo BUILD FAILED! Check errors above.
    echo ========================================================
)
