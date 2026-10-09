$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    Write-Host "C# compiler not found at $csc" -ForegroundColor Red
    exit 1
}

Write-Host "Compiling Banglish.exe for Windows..." -ForegroundColor Green

& $csc /nologo /target:winexe /out:Banglish.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll src\AvroData.cs src\BanglishEngine.cs src\Win32Caret.cs src\KeyboardHook.cs src\CandidateForm.cs src\TrayApp.cs

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBUILD SUCCESSFUL! Output: windows\Banglish.exe" -ForegroundColor Green
} else {
    Write-Host "`nBUILD FAILED!" -ForegroundColor Red
}
