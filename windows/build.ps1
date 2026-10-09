$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    Write-Host "C# compiler not found at $csc" -ForegroundColor Red
    exit 1
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "1. Compiling Banglish.exe (Main Application)..." -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Cyan

& $csc /nologo /target:winexe /win32icon:app.ico /out:Banglish.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll /r:System.Core.dll src\AvroData.cs src\BanglishEngine.cs src\BanglishDictionary.cs src\Win32Caret.cs src\KeyboardHook.cs src\CandidateForm.cs src\WelcomeForm.cs src\TrayApp.cs

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nFailed to build Banglish.exe!" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "2. Compiling Banglish-Setup.exe (Setup Wizard)..." -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Cyan

& $csc /nologo /target:winexe /win32icon:app.ico /out:Banglish-Setup.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll src\SetupWizard.cs

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "  -> windows\Banglish.exe" -ForegroundColor Yellow
    Write-Host "  -> windows\Banglish-Setup.exe" -ForegroundColor Yellow
} else {
    Write-Host "`nFailed to build Banglish-Setup.exe!" -ForegroundColor Red
}
