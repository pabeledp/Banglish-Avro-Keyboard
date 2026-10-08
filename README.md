# Banglish (বাংলিশ) — Native Avro Phonetic Bangla Keyboard for macOS

<div align="center">
  <img src="Banglish-Logo.png" alt="Banglish Logo" width="180">
  
  <h3>১০০% নেটিভ বাংলা ফোনেটিক কীবোর্ড — Built Natively for Apple Silicon & Intel Macs</h3>
  
  <p>
    <a href="https://github.com/pabeledp/Banglish-Avro-Keyboard/raw/main/Banglish_Installer.dmg">
      <img src="https://img.shields.io/badge/Download-Banglish_Installer.dmg-FF3B30?style=for-the-badge&logo=apple&logoColor=white" alt="Download DMG">
    </a>
    &nbsp;
    <a href="https://github.com/pabeledp/Banglish-Avro-Keyboard/raw/main/Banglish_Installer.pkg">
      <img src="https://img.shields.io/badge/Download-Install_Banglish.pkg-007AFF?style=for-the-badge&logo=apple&logoColor=white" alt="Download PKG">
    </a>
  </p>

  <p>
    <img src="https://img.shields.io/badge/macOS-12.0%2B-black?logo=apple" alt="macOS 12+">
    <img src="https://img.shields.io/badge/Apple%20Silicon-M1%20%7C%20M2%20%7C%20M3%20%7C%20M4%20%7C%20M5-success" alt="Apple Silicon">
    <img src="https://img.shields.io/badge/Language-Swift%20100%25-orange?logo=swift" alt="Pure Swift">
    <img src="https://img.shields.io/badge/Rosetta-Not%20Required-brightgreen" alt="No Rosetta">
    <img src="https://img.shields.io/badge/Privacy-100%25%20Offline-blue" alt="100% Offline">
  </p>
</div>

---

## 🌟 Why Banglish?

**Banglish** is a 100% native macOS Input Method Editor (IME) built entirely in Swift using Apple's official **InputMethodKit (IMK)** framework. 

Unlike legacy or ported tools:
* 🚀 **100% Pure Native Swift** — Zero external C/Rust dependencies, ultra-lightweight, and lightning-fast.
* ⚡ **Apple Silicon Native** — Built specifically for Apple M-series chips (M1–M5) as well as modern Intel Macs. Zero Rosetta translation overhead.
* 🎨 **Liquid Glass UI Dashboard** — Clean Apple-inspired frosted glass settings panel and interactive typing playground.
* 🎬 **Full Creative App Support** — Seamlessly types into **Adobe After Effects, Photoshop, Illustrator, Premiere Pro**, Figma, Final Cut Pro, Chrome, Safari, Notes, and Terminal.
* 🔒 **Completely Offline & Private** — No network permissions, no telemetry, no background data collection. Your keystrokes never leave your Mac.

---

## 📥 Downloads

| Installer | Description | Download Link |
| :--- | :--- | :--- |
| **Banglish Disk Image (.dmg)** | Drag-and-drop installer disk image | [⬇️ Download Banglish_Installer.dmg](https://github.com/pabeledp/Banglish-Avro-Keyboard/raw/main/Banglish_Installer.dmg) |
| **Banglish Package (.pkg)** | Automated one-click system package | [⬇️ Download Banglish_Installer.pkg](https://github.com/pabeledp/Banglish-Avro-Keyboard/raw/main/Banglish_Installer.pkg) |

---

## 🚀 Installation Guide

### Option 1: Using the Installer (Recommended)
1. Download **`Banglish_Installer.dmg`** or **`Banglish_Installer.pkg`**.
2. Open the file and run the installer.
3. **Important (First Time Setup):**
   * Log out of your macOS account and log back in (or Restart your Mac).  
     *(macOS requires a session refresh to register new third-party input methods in the settings list).*
4. Open **System Settings** ➔ **Keyboard** ➔ **Text Input** ➔ Click **Edit...** (or Input Sources).
5. Click the **(+)** button at the bottom left.
6. Select **Bangla** from the language list ➔ Select **Banglish** ➔ Click **Add**.
7. Now switch to **Banglish** from the macOS top menu bar keyboard icon (or press `Ctrl + Space` / Globe key).

---

## 🎬 Adobe After Effects & Creative Cloud Setup

To type Bengali Unicode in **Adobe After Effects, Photoshop, Illustrator, or Premiere** without broken conjuncts or reversed letters:

1. Open **After Effects**.
2. Go to top menu: **After Effects** ➔ **Settings** (or **Preferences**) ➔ **Type**.
3. Under **Text Engine** / **Language Options**, select **"South Asian and Middle Eastern"** (or Indic).
4. Restart After Effects.
5. In your composition, pick the Type Tool (`Cmd + T`) and select a Unicode Bengali font (such as **Kalpurush**, **Hind Siliguri**, **Noto Sans Bengali**, or **Adobe Bengali**).
6. Switch input source to **Banglish** and type!

---

## ⌨️ Avro Phonetic Typing Cheatsheet

Banglish uses the standard, intuitive **Avro Phonetic** keyboard layout:

| You Type | Bangla Output | Meaning |
| :--- | :--- | :--- |
| `ami` | **আমি** | I |
| `bangla` | **বাংলা** | Bengali |
| `kemon` | **কেমন** | How |
| `desh` | **দেশ** | Country |
| `dhaka` | **ঢাকা** | Dhaka |
| `bhalo` | **ভালো** | Good |
| `kormo` | **কর্ম** | Work / Karma |
| `shundor` | **সুন্দর** | Beautiful |
| `ruti` | **রুটি** | Bread |
| `.` | **।** | Daari (Bengali period) |
| `$` | **৳** | Taka sign |

### Useful Hotkeys
* `⌥ + Space` (Option + Space): Toggle between **Bangla** and **English** on the fly.
* `Ctrl + Space`: Cycle through active macOS input sources.

---

## 🛠️ Building from Source

To build Banglish locally on your Mac:

```bash
# Clone the repository
git clone https://github.com/pabeledp/Banglish-Avro-Keyboard.git
cd Banglish-Avro-Keyboard

# Build the release DMG and PKG
bash scripts/build_release_dmg.sh
```

---

## 📂 Project Structure

```
Banglish-Avro-Keyboard/
├── Banglish-Logo.png               # Official Banglish Branding Logo
├── Banglish_Installer.dmg          # Ready-to-use DMG Installer
├── Banglish_Installer.pkg          # Ready-to-use PKG Installer
├── fix_banglish_now.sh             # Quick system registration & setup script
├── scripts/
│   ├── build_release_dmg.sh        # Release packaging script
│   └── build_and_install.sh        # Local development build script
├── src/
│   ├── AvroData.swift              # Full Avro phonetic grammar & phonetic patterns
│   ├── BanglishEngine.swift        # Pure Swift phonetic transliteration engine
│   ├── BanglishAppDelegate.swift   # Menu bar coordinator & mode manager
│   ├── BanglishInputController.swift# macOS IMKInputController event handling
│   ├── AppUI.swift                 # Apple Liquid Glass dashboard interface
│   └── main.swift                  # IMKServer bootstrap entrypoint
└── Resources/                      # High-res AppIcon.icns & MenuIcon assets
```

---

## 📄 License

Open-source under the **MIT License**. Created by [A M Pabel](https://github.com/pabeledp).
