# Banglish — Native Avro Phonetic Bangla Keyboard for macOS

<div align="center">
  <img src="Banglish-Logo.png" alt="Banglish Logo" width="160">
  
  <p><strong>100% Native Bangla Phonetic Input Method Editor for macOS</strong><br>Built natively for Apple Silicon (M1/M2/M3/M4/M5) and Intel Macs</p>
  
  <p>
    <a href="https://github.com/pabeledp/Banglish-Avro-Keyboard/releases/latest/download/Banglish.dmg">
      <img src="https://img.shields.io/badge/Download%20Banglish-macOS-000000?style=for-the-badge&logo=apple&logoColor=white" alt="Download Banglish for macOS">
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
* **Smart Word Suggestions & Candidate Panel:** Instant suggestions for complex conjuncts (যুক্তবর্ণ), vowel lengths (ি/ী, ু/ূ), and orthographic variants with keyboard & mouse selection.
* **Apple Silicon Optimized:** Instant launch, zero CPU overhead at idle, and completely free of Rosetta.
* **Creative & Pro Apps Ready:** Works smoothly in Adobe After Effects, Photoshop, Illustrator, Premiere Pro, Figma, Final Cut Pro, Chrome, Safari, Notes, and Terminal.
* **Completely Private & Offline:** No internet permissions, no data collection, and zero telemetry.

---

## Installation

### Step 1: Install
Download and open **`Banglish.dmg`**, then double-click the installer.

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

## Phonetic Typing Guide & Key Chart (টাইপিং গাইড ও বর্ণ চার্ট)

Banglish implements the standard Avro Phonetic scheme. Here is the complete, clean reference guide for vowels, consonants, modifiers (ফলা), and complex conjuncts (যুক্তাক্ষর).

### ১. স্বরবর্ণ ও কার চিহ্ন (Vowels & Signs)

| English Key | বাংলা স্বরবর্ণ | কার রূপ | উদাহরণ (Example) |
| :--- | :---: | :---: | :--- |
| `o` / `a` | **অ** | *(উহ্য)* | `onek` $\rightarrow$ অনেক |
| `a` / `A` / `aa` | **আ** | **া** | `aakash` $\rightarrow$ আকাশ, `baba` $\rightarrow$ বাবা |
| `i` | **ই** | **ি** | `iti` $\rightarrow$ ইতি, `kintu` $\rightarrow$ কিন্তু |
| `I` / `ee` | **ঈ** | **ী** | `Ishwar` $\rightarrow$ ঈশ্বর, `nodI` $\rightarrow$ নদী |
| `u` | **উ** | **ু** | `upor` $\rightarrow$ উপর, `tumi` $\rightarrow$ তুমি |
| `U` / `oo` | **ঊ** | **ূ** | `Usha` $\rightarrow$ ঊষা, `mUlo` $\rightarrow$ মূলো |
| `rri` | **ঋ** | **ৃ** | `rritu` $\rightarrow$ ঋতু, `kripa` $\rightarrow$ কৃপা |
| `e` | **এ** | **ে** | `ekhon` $\rightarrow$ এখন, `desh` $\rightarrow$ দেশ |
| `OI` / `oi` | **ঐ** | **ৈ** | `Oikyo` $\rightarrow$ ঐক্য, `toiri` $\rightarrow$ তৈরি |
| `O` / `o` | **ও** | **ো** | `olpo` $\rightarrow$ অল্প, `kothao` $\rightarrow$ কোথাও |
| `OU` / `ou` | **ঔ** | **ৌ** | `OUshodh` $\rightarrow$ ঔষধ, `pOUsh` $\rightarrow$ পৌষ |

---

### ২. ব্যঞ্জনবর্ণ (Consonants)

| Key | বর্ণ | Key | বর্ণ | Key | বর্ণ | Key | বর্ণ | Key | বর্ণ |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `k` | **ক** | `kh` | **খ** | `g` | **গ** | `gh` | **ঘ** | `Ng` | **ঙ** |
| `c` / `ch` | **চ** | `Ch` | **ছ** | `j` | **জ** | `jh` | **ঝ** | `NG` | **ঞ** |
| `T` | **ট** | `Th` | **ঠ** | `D` | **ড** | `Dh` | **ঢ** | `N` | **ণ** |
| `t` | **ত** | `th` | **থ** | `d` | **দ** | `dh` | **ধ** | `n` | **ন** |
| `p` | **প** | `f` / `ph` | **ফ** | `b` | **ব** | `bh` / `v` | **ভ** | `m` | **ম** |
| `z` / `y` | **য** | `r` | **র** | `l` | **ল** | `sh` / `S` | **শ** | `Sh` | **ষ** |
| `s` | **স** | `h` | **হ** | `R` | **ড়** | `Rh` | **ঢ়** | `y` / `Y` | **য়** |
| `t`` | **ৎ** | `ng` | **ং** | `:`` | **ঃ** | `^` / `**` | **ঁ** | `.` | **।** |

---

### ৩. ফলা ও বিশেষ চিহ্ন (Folas & Modifiers)

* **রেফ (র্):** ব্যঞ্জনের আগে বা পরে `rr` চাপলে রেফ বসে। (যেমন: `kormo` বা `korr` $\rightarrow$ **কর্ম**, `torrko` $\rightarrow$ **তর্ক**, `borrsho` $\rightarrow$ **বর্ষ**)
* **র-ফলা (্র):** ব্যঞ্জনের পর `r` চাপলে র-ফলা হয়। (যেমন: `gram` $\rightarrow$ **গ্রাম**, `probhu` $\rightarrow$ **প্রভু**, `chiro` $\rightarrow$ **চির**)
* **য-ফলা (্য):** ব্যঞ্জনের পর `y` বা `Z` চাপলে য-ফলা হয়। (যেমন: `baky` $\rightarrow$ **বাক্য**, `onny` $\rightarrow$ **অন্য**, `kya` $\rightarrow$ **ক্যা**)
* **ব-ফলা (্ব):** ব্যঞ্জনের পর `w` বা `b` চাপলে ব-ফলা হয়। (যেমন: `bissho` / `biswo` $\rightarrow$ **বিশ্ব**, `dhwoni` $\rightarrow$ **ধ্বনি**)
* **ম-ফলা (্ম):** ব্যঞ্জনের পর `m` চাপলে ম-ফলা হয়। (যেমন: `padma` / `podmo` $\rightarrow$ **পদ্ম**, `atmo` $\rightarrow$ **আত্ম**)
* **ন/ণ-ফলা (্ন/্ণ):** ব্যঞ্জনের পর `n` বা `N` চাপলে ন/ণ-ফলা হয়। (যেমন: `roshno` $\rightarrow$ **প্রশ্ন**, `oporanho` $\rightarrow$ **অপরাহ্ণ**)
* **চন্দ্রবিন্দু (ঁ):** বানানের সাথে `^` (Shift + 6) চাপলে সরাসরি চন্দ্রবিন্দু বসে। সাধারণ বানানেও স্মার্ট সাজেশন স্বয়ংক্রিয়ভাবে চন্দ্রবিন্দু যুক্ত শব্দ দেখায়:
  - `chad` $\rightarrow$ **চাঁদ**
  - `has` $\rightarrow$ **হাঁস**
  - `badha` $\rightarrow$ **বাঁধা**
  - `kacha` $\rightarrow$ **কাঁচা**
  - `pach` $\rightarrow$ **পাঁচ**
  - `bash` $\rightarrow$ **বাঁশ**
  - `dat` $\rightarrow$ **দাঁত**
  - `hatu` $\rightarrow$ **হাঁটু**
* **হসন্ত (্):** দুটি অক্ষরের মাঝে কমা `,` চাপলে জোরপূর্বক হসন্ত বসে (যেমন: `h,s` $\rightarrow$ **হ্‌স**)।

---

### ৪. জটিল যুক্তাক্ষর লেখার গাইড (Complex Conjuncts)

বাংলা ভাষার জটিল ও বিরল যুক্তবর্ণসমূহ সহজে লেখার নিয়ম:

| যুক্তাক্ষর | মূল অক্ষরসমূহ | টাইপিং সিকোয়েন্স | উদাহরণ (Examples) |
| :--- | :--- | :--- | :--- |
| **ক্ষ** | ক + ষ | `kkh` | `kkhoma` $\rightarrow$ **ক্ষমা**, `shikkha` $\rightarrow$ **শিক্ষা** |
| **ক্ষ্ম** | ক + ষ + ম | `kkhm` | `shukkho` / `sukkhmo` $\rightarrow$ **সূক্ষ্ম**, `lokshmi` $\rightarrow$ **লক্ষ্মী** |
| **জ্ঞ** | জ + ঞ | `jng` / `gy` / `jnan` | `gyan` / `jnan` $\rightarrow$ **জ্ঞান**, `biggan` $\rightarrow$ **বিজ্ঞান** |
| **ঞ্চ** | ঞ + চ | `nch` / `NGc` | `onchol` $\rightarrow$ **অঞ্চল**, `chonchol` $\rightarrow$ **চঞ্চল** |
| **ঞ্ছ** | ঞ + ছ | `nCh` / `NGCh` | `lanchona` $\rightarrow$ **লাঞ্ছনা**, `banchito` $\rightarrow$ **বাঞ্ছিত** |
| **ঞ্জ** | ঞ + জ | `nj` / `NGj` | `gonj` $\rightarrow$ **গঞ্জ**, `ronjon` $\rightarrow$ **রঞ্জন** |
| **ঙ্ক** | ঙ + ক | `ngk` / `Ngk` | `onko` $\rightarrow$ **অঙ্ক**, `atongko` $\rightarrow$ **আতঙ্ক** |
| **ঙ্খ** | ঙ + খ | `ngkh` / `Ngkh` | `shongkho` $\rightarrow$ **শঙ্খ** |
| **ঙ্গ** | ঙ + গ | `ngg` / `Ngg` | `bongo` $\rightarrow$ **বঙ্গ**, `shongo` $\rightarrow$ **সঙ্গ** |
| **ঙ্ঘ** | ঙ + ঘ | `nggh` / `Nggh` | `jonggho` $\rightarrow$ **জঙ্ঘা**, `kriddho` |
| **ষ্ণ** | ষ + ণ | `ShN` / `shn` | `krishna` $\rightarrow$ **কৃষ্ণ**, `ushno` $\rightarrow$ **উষ্ণ** |
| **হ্ম** | হ + ম | `hm` | `brahmon` $\rightarrow$ **ব্রাহ্মণ** |
| **হ্ন** | হ + ন | `hn` | `chinho` $\rightarrow$ **চিহ্ন**, `bonhi` $\rightarrow$ **বহ্নি** |
| **হ্ণ** | হ + ণ | `hN` | `oporanho` $\rightarrow$ **অপরাহ্ণ**, `purbanho` $\rightarrow$ **পূর্বাহ্ণ** |
| **হ্ল** | হ + ল | `hl` | `ahlad` $\rightarrow$ **আহ্লাদ** |
| **হ্র** | হ + র | `hr` | `hroswo` $\rightarrow$ **হ্রস্ব** |
| **হৃ** | হ + ঋ | `hri` / `hrri` | `hridoy` $\rightarrow$ **হৃদয়** |
| **দ্ধ** | দ + ধ | `ddh` | `juddho` $\rightarrow$ **যুদ্ধ**, `buddhi` $\rightarrow$ **বুদ্ধি** |
| **ব্ধ** | ব + ধ | `bdh` | `stobdho` $\rightarrow$ **স্তব্ধ**, `lubdho` $\rightarrow$ **লুব্ধ** |
| **ব্ভ** | ব + ভ | `bbh` | `ubbhot` $\rightarrow$ **উদ্ভট** |
| **ত্ত** | ত + ত | `tt` | `uttor` $\rightarrow$ **উত্তর**, `britto` $\rightarrow$ **বৃত্ত** |
| **ত্থ** | ত + থ | `tth` | `utthan` $\rightarrow$ **উত্থান** |
| **ত্ম** | ত + ম | `tm` | `atmo` $\rightarrow$ **আত্ম**, `atmiyo` $\rightarrow$ **আত্মীয়** |
| **ক্ত** | ক + ত | `kt` | `rokto` $\rightarrow$ **রক্ত**, `mukto` $\rightarrow$ **মুক্ত** |
| **ণ্ড** | ণ + ড | `ND` / `nd` | `chondi` $\rightarrow$ **চণ্ডী**, `dondo` $\rightarrow$ **দণ্ড** |
| **স্থ** | স + থ | `sth` | `sthan` $\rightarrow$ **স্থান**, `sustho` $\rightarrow$ **সুস্থ** |
| **স্প** | স + প | `sp` | `sporsho` $\rightarrow$ **স্পর্শ**, `sposhto` $\rightarrow$ **স্পষ্ট** |
| **শ্র** | শ + র | `sr` / `shr` | `srom` $\rightarrow$ **শ্রম**, `sroddha` $\rightarrow$ **শ্রদ্ধা** |
| **স্র** | স + র | `sr` | `sroshta` $\rightarrow$ **স্রষ্টা**, `srot` $\rightarrow$ **স্রোত** |

---

### ৫. শর্টকাট ও বিবিধ (Shortcuts & Symbols)

* `⌥ + Space` (Option + Space): বাংলা ও ইংলিশ মোডের মধ্যে দ্রুত টগল।
* `.` $\rightarrow$ **।** (বাংলা দাঁড়ি)
* `..` $\rightarrow$ **.** (ইংরেজি ডট/ফুলস্টপ)
* `$` $\rightarrow$ **৳** (টাকা চিহ্ন)
* **সাজেশন পপআপ নেভিগেশন:** মাউস দিয়ে ক্লিক করে অথবা কীবোর্ডের `1` থেকে `9` নম্বর কি চেপে সরাসরি সাজেশন নির্বাচন করা যায়। পছন্দের শব্দ শীর্ষে থাকলে সরাসরি `Space` বা `Enter` চাপলে তা টাইপ হয়ে যাবে।

---

## Releases & Older Versions

The primary download button at the top of this repository provides the latest release. If you wish to download a previous or classic version:

<p>
  <a href="https://github.com/pabeledp/Banglish-Avro-Keyboard/releases">
    <img src="https://img.shields.io/badge/Browse%20All%20Releases-Download%20Older%20Versions-000000?style=for-the-badge&logo=github&logoColor=white" alt="Download Older Versions">
  </a>
</p>

| Version | Release Highlights | Direct Download |
| :--- | :--- | :--- |
| **v1.0.001** *(Latest)* | Smart Word Suggestions, Dark Green & White Theme, Auto Reph/Ro-fola | [Banglish-v1.0.001.dmg](https://github.com/pabeledp/Banglish-Avro-Keyboard/releases/download/v1.0.001/Banglish-v1.0.001.dmg) |
| **v1.0.0** *(Classic)* | 100% Native Pure Swift Avro Engine (Direct typing without suggestions popup) | [Banglish-v1.0.0.dmg](https://github.com/pabeledp/Banglish-Avro-Keyboard/releases/download/v1.0.0/Banglish-v1.0.0.dmg) |

---

## License

Released under the **MIT License**. Created by [A M Pabel](https://github.com/pabeledp).
