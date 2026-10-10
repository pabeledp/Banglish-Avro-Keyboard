$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    Write-Host "C# compiler not found at $csc" -ForegroundColor Red
    exit 1
}

# Ensure dictionary compressed bundle exists
if (-not (Test-Path "words.txt.gz") -or ((Get-Item "words.txt").LastWriteTime -gt (Get-Item "words.txt.gz").LastWriteTime)) {
    Write-Host "Compressing words.txt to words.txt.gz..." -ForegroundColor Yellow
    $compressCode = @"
using System;
using System.IO;
using System.IO.Compression;
class DictComp {
    static void Main() {
        byte[] raw = File.ReadAllBytes("words.txt");
        using (FileStream fs = new FileStream("words.txt.gz", FileMode.Create))
        using (GZipStream gz = new GZipStream(fs, CompressionMode.Compress)) {
            gz.Write(raw, 0, raw.Length);
        }
    }
}
"@
    Set-Content -Path "dictcomp.cs" -Value $compressCode
    & $csc /nologo /out:dictcomp.exe dictcomp.cs
    .\dictcomp.exe
    Remove-Item dictcomp.cs, dictcomp.exe -ErrorAction SilentlyContinue
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "1. Compiling Banglish.exe (Main Application)..." -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Cyan

& $csc /nologo /target:winexe /platform:anycpu /win32icon:app.ico /resource:Banglish-Logo.png /resource:app.ico /resource:words.txt.gz /resource:autodict.txt /out:Banglish.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll /r:System.Core.dll src\AvroData.cs src\BanglishEngine.cs src\BanglishDictionary.cs src\Win32Caret.cs src\KeyboardHook.cs src\CandidateForm.cs src\ToggleBarForm.cs src\WelcomeForm.cs src\TrayApp.cs

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nFailed to build Banglish.exe!" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "2. Compiling Banglish-Setup.exe (Standalone Setup Wizard)..." -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Cyan

& $csc /nologo /target:winexe /platform:anycpu /win32icon:app.ico /resource:Banglish-Logo.png /resource:app.ico /resource:Banglish.exe /resource:words.txt.gz /resource:autodict.txt /out:Banglish-Setup.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll src\SetupWizard.cs

if ($LASTEXITCODE -eq 0) {
    Copy-Item "Banglish-Setup.exe" "Banglish-Setup-v1.0.0.exe" -Force
    Write-Host "`nBUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "  -> windows\Banglish.exe (Standalone)" -ForegroundColor Yellow
    Write-Host "  -> windows\Banglish-Setup.exe (Self-Contained Installer)" -ForegroundColor Yellow
    Write-Host "  -> windows\Banglish-Setup-v1.0.0.exe" -ForegroundColor Yellow
} else {
    Write-Host "`nFailed to build Banglish-Setup.exe!" -ForegroundColor Red
}
