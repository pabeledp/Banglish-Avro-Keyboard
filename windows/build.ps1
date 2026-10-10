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

# Common source files
$coreSrc = "src\AvroData.cs src\BanglishEngine.cs src\BanglishDictionary.cs src\Win32Caret.cs src\KeyboardHook.cs src\CandidateForm.cs src\ToggleMenuForm.cs src\ToggleBarForm.cs src\WelcomeForm.cs src\TrayApp.cs"
$voiceSrc = "src\Voice\BanglishConfig.cs src\Voice\AudioRecorder.cs src\Voice\GoogleSpeechClient.cs src\Voice\VoiceIndicatorForm.cs src\Voice\VoiceTypingManager.cs"

# ========================================================
# 1. BUILD STABLE RELEASE: v1.0.0 (Main Download Files)
# ========================================================
Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "1. Building STABLE v1.0.0 Release (Main Default Version)..." -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Cyan

cmd.exe /c "$csc /nologo /target:winexe /platform:anycpu /win32icon:app.ico /resource:Banglish-Logo.png /resource:app.ico /resource:words.txt.gz /resource:autodict.txt /out:Banglish.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll /r:System.Core.dll $coreSrc"

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nFailed to build stable Banglish.exe!" -ForegroundColor Red
    exit 1
}

cmd.exe /c "$csc /nologo /target:winexe /platform:anycpu /win32icon:app.ico /resource:Banglish-Logo.png /resource:app.ico /resource:Banglish.exe /resource:words.txt.gz /resource:autodict.txt /out:Banglish-Setup.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll src\SetupWizard.cs"

if ($LASTEXITCODE -eq 0) {
    Copy-Item "Banglish-Setup.exe" "Banglish-Setup-v1.0.0.exe" -Force
    Write-Host "  [OK] windows\Banglish.exe (v1.0.0 Stable)" -ForegroundColor Yellow
    Write-Host "  [OK] windows\Banglish-Setup.exe (v1.0.0 Stable - Main Download)" -ForegroundColor Yellow
    Write-Host "  [OK] windows\Banglish-Setup-v1.0.0.exe (v1.0.0 Archive)" -ForegroundColor Yellow
} else {
    Write-Host "`nFailed to build stable Banglish-Setup.exe!" -ForegroundColor Red
    exit 1
}

# ========================================================
# 2. BUILD BETA RELEASE: v1.0.1-beta (Voice Typing Feature)
# ========================================================
Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "2. Building BETA v1.0.1-beta Release (Voice Typing)..." -ForegroundColor Magenta
Write-Host "========================================================" -ForegroundColor Cyan

cmd.exe /c "$csc /nologo /target:winexe /define:VOICE_BETA /platform:anycpu /win32icon:app.ico /resource:Banglish-Logo.png /resource:app.ico /resource:words.txt.gz /resource:autodict.txt /out:Banglish-v1.0.1-beta.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll /r:System.Core.dll $coreSrc $voiceSrc"

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nFailed to build Banglish-v1.0.1-beta.exe!" -ForegroundColor Red
    exit 1
}

cmd.exe /c "$csc /nologo /target:winexe /platform:anycpu /win32icon:app.ico /resource:Banglish-Logo.png /resource:app.ico /resource:Banglish-v1.0.1-beta.exe /resource:words.txt.gz /resource:autodict.txt /out:Banglish-Setup-v1.0.1-beta.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll src\SetupWizard.cs"

if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] windows\Banglish-v1.0.1-beta.exe (Beta Executable)" -ForegroundColor Magenta
    Write-Host "  [OK] windows\Banglish-Setup-v1.0.1-beta.exe (Beta Setup Wizard)" -ForegroundColor Magenta
    Write-Host "`nALL BUILDS COMPLETED SUCCESSFULLY!" -ForegroundColor Green
} else {
    Write-Host "`nFailed to build Banglish-Setup-v1.0.1-beta.exe!" -ForegroundColor Red
}
