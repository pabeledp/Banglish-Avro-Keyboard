# Banglish — Native Avro Phonetic Bangla Keyboard for macOS

<div align="center">
  <img src="Banglish-Logo.png" alt="Banglish Logo" width="160">
  
  <p><strong>100% Native Bangla Phonetic Input Method Editor for macOS</strong><br>Built natively for Apple Silicon (M1/M2/M3/M4/M5) and Intel Macs</p>
  
  <p>
    <a href="https://github.com/pabeledp/Banglish-Avro-Keyboard/raw/main/Banglish_Installer.dmg">
      <img src="https://img.shields.io/badge/Download%20for%20macOS-Banglish__Installer.dmg-000000?style=for-the-badge&logo=apple&logoColor=white" alt="Download Banglish for macOS">
    </a>
  </p>

  <p>
    <img src="https://img.shields.io/badge/macOS-12.0%2B-000000?style=flat-square&logo=apple&logoColor=white" alt="macOS 12+">
    <img src="https://img.shields.io/badge/Apple%20Silicon-Native-000000?style=flat-square" alt="Apple Silicon">
    <img src="https://img.shields.io/badge/Swift-100%25-000000?style=flat-square&logo=swift&logoColor=white" alt="Pure Swift">
    <img src="https://img.shields.io/badge/Rosetta-Not%20Required-000000?style=flat-square" alt="No Rosetta">
    <img src="https://img.shields.io/badge/Privacy-100%25%20Offline-000000?style=flat-square" alt="100% Offline">
  </p>
</div>

---

## Tribute to Avro Keyboard

> *"Aamader mukhe mukhe gaan, bhashar bandhon katuk byabodhan."*

The revolution of modern Bangla typing began with **Dr. Mehdi Hasan Khan** and **OmicronLab**, whose legendary creation **Avro Keyboard** empowered millions of Bengalis around the world to write freely in their mother tongue. 

The Banglish team extends its deepest gratitude and utmost respect to Dr. Mehdi Hasan Khan and the entire OmicronLab team. Banglish proudly honors and adopts the phonetic input principles pioneered by Avro.

---

## Why Banglish?

The classic **iAvro** project served Mac users for years, but has remained unmaintained for a very long time. With modern macOS updates—most notably **macOS 27 (Golden Gate)** and Apple winding down Intel/Rosetta translation on Apple Silicon—no official updates or native modern releases have arrived for Mac.

To ensure that Mac users continue to enjoy a seamless, future-proof Bengali typing experience, we built **Banglish** as an independent, 100% native modern alternative specifically for macOS:

* **100% Pure Swift & InputMethodKit:** Zero third-party C or Rust dependencies. Runs directly as an official macOS input method.
* **Apple Silicon Optimized:** Instant launch, zero CPU overhead at idle, and completely free of Rosetta.
* **Creative & Pro Apps Ready:** Works smoothly in Adobe After Effects, Photoshop, Illustrator, Premiere Pro, Figma, Final Cut Pro, Chrome, Safari, Notes, and Terminal.
* **Completely Private & Offline:** No internet permissions, no data collection, and zero telemetry.

---

## Installation

### Step 1: Install
Download and open **`Banglish_Installer.dmg`**, then double-click the installer.

### Step 2: Log Out and Log Back In *(Required)*
Log out of your macOS account and log back in (or restart your Mac).  
> *Note: macOS security policy requires a session refresh to index newly installed third-party input methods in system preferences.*

### Step 3: Add Banglish
1. Open **System Settings** ➔ **Keyboard** ➔ **Text Input** ➔ Click **Edit...** (or Input Sources).
2. Click the **(+)** button at the bottom left.
3. Select **Bangla** from the language list, select **Banglish**, and click **Add**.
4. Select **Banglish** from your macOS menu bar keyboard menu (or press `Ctrl + Space` / Globe key) and begin typing.

---

## Adobe After Effects & Creative Cloud Setup

To ensure Bengali conjuncts (যুক্তাক্ষর) and vowel signs render properly without breaking in Adobe After Effects, Photoshop, or Premiere:

1. Open **After Effects**.
2. Navigate to: **After Effects** ➔ **Settings** (or **Preferences**) ➔ **Type**.
3. Under **Text Engine** / **Language Options**, select **"South Asian and Middle Eastern"** (or Indic).
4. Restart After Effects.
5. In your composition, create a text layer with the Type Tool (`Cmd + T`) and choose a Unicode Bengali font (such as **Kalpurush**, **Hind Siliguri**, **Noto Sans Bengali**, or **Adobe Bengali**).
6. Switch input source to **Banglish** and type phonetically!

---

## Phonetic Cheatsheet

Banglish implements the familiar Avro Phonetic scheme:

| Keystroke | Bangla Output | Meaning |
| :--- | :--- | :--- |
| `ami` | **আমি** | I |
| `bangla` | **বাংলা** | Bangla |
| `kemon` | **কেমন** | How |
| `desh` | **দেশ** | Country |
| `dhaka` | **ঢাকা** | Dhaka |
| `bhalo` | **ভালো** | Good |
| `kormo` | **কর্ম** | Work |
| `shundor` | **সুন্দর** | Beautiful |
| `ruti` | **রুটি** | Bread |
| `.` | **।** | Daari (Full stop) |
| `$` | **৳** | Taka symbol |

### Shortcuts
* `⌥ + Space` (Option + Space): Toggle between **Bangla** and **English** passthrough mode.
* `Ctrl + Space`: Cycle between active macOS input sources.

---

## License

Released under the **MIT License**. Created by [A M Pabel](https://github.com/pabeledp).
