# Banglish for Windows (বাংলা কীবোর্ড)

**Banglish for Windows** is a 100% native, ultra-fast, lightweight phonetic Bengali (Bangla) keyboard application for Windows 10 & 11.

It provides the familiar Avro-style phonetic typing experience (`ami banglay gan gai` → `আমি বাংলায় গান গাই`) across all Windows applications with zero installation overhead, zero external dependencies, and 100% offline security.

---

## 🌟 Key Features

* ⚡ **100% Native & Lightweight**: Zero external runtime requirements (.NET 4.8 Built-in Win32 API).
* ⌨️ **System-Wide Typing Hook**: Works in all Windows applications (Notepad, Chrome, Word, VS Code, Discord, WhatsApp, Messenger, Terminal, etc.).
* 🔄 **Instant Hotkey Switch (`F12`)**: Switch instantly between **English** and **Banglish (বাংলা)** typing modes anytime.
* 🎈 **Floating Overlay & System Tray**: Live composition overlay displaying typed text and instant visual status in the notification tray.
* 🔒 **100% Offline & Private**: Zero telemetry, no network calls, no keylogging risk.

---

## 🚀 How to Run

1. Download or compile `Banglish.exe`.
2. Double-click `Banglish.exe`. Banglish will launch in your System Tray (Taskbar Notification area).
3. Press **`F12`** to toggle Banglish mode ON/OFF.
4. Type in any application!

---

## 🛠️ Building from Source

No Visual Studio or heavy SDKs required! You can compile `Banglish.exe` directly using Windows' built-in C# compiler (`csc.exe`):

### Using PowerShell Script:
```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

### Or direct C# compiler command:
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:Banglish.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll src\AvroData.cs src\BanglishEngine.cs src\KeyboardHook.cs src\CandidateForm.cs src\TrayApp.cs
```

---

## 📜 Phonetic Typing Examples

| Phonetic Input (Banglish) | Bangla Output |
| :--- | :--- |
| `ami banglay gan gai` | আমি বাংলায় গান গাই |
| `amar shonar bangla` | আমার সোনার বাংলা |
| `kemon acho?` | কেমন আছো? |
| `khobor` | খবর |
| `obhinondon` | অভিনন্দন |
| `shadhinota` | স্বাধীনতা |

---

## 📄 License & Credits
* Created by **A M Pabel** (`@pabeledp`) & **Team FramEmpire**.
* Open Source under the MIT License.
