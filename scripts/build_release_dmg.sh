#!/usr/bin/env bash
set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP_NAME="Banglish"
VERSION="1.0.001"
BUILD_DIR="${PROJECT_DIR}/build"
APP_DIR="${BUILD_DIR}/${APP_NAME}.app"
PKG_DIR="${PROJECT_DIR}/build/pkg_root"
DMG_STAGING="${PROJECT_DIR}/build/dmg_release"
DMG_OUTPUT="${PROJECT_DIR}/${APP_NAME}-v${VERSION}.dmg"
PKG_OUTPUT="${PROJECT_DIR}/${APP_NAME}-v${VERSION}.pkg"


echo "=========================================="
echo " Building ${APP_NAME} Release DMG"
echo "=========================================="

rm -rf "${BUILD_DIR}"
mkdir -p "${APP_DIR}/Contents/MacOS"
mkdir -p "${APP_DIR}/Contents/Resources/English.lproj"

# 1. Compile native Swift binary
echo "-> Compiling Swift sources..."
SDK_PATH="/Library/Developer/CommandLineTools/SDKs/MacOSX.sdk"
/Library/Developer/CommandLineTools/usr/bin/swiftc \
    -O \
    -sdk "${SDK_PATH}" \
    -target arm64-apple-macos12.0 \
    -framework Cocoa \
    -framework InputMethodKit \
    "${PROJECT_DIR}/src/AvroData.swift" \
    "${PROJECT_DIR}/src/BanglishEngine.swift" \
    "${PROJECT_DIR}/src/BanglishDictionary.swift" \
    "${PROJECT_DIR}/src/CandidateWindow.swift" \
    "${PROJECT_DIR}/src/BanglishInputController.swift" \
    "${PROJECT_DIR}/src/AppUI.swift" \
    "${PROJECT_DIR}/src/main.swift" \
    -o "${APP_DIR}/Contents/MacOS/${APP_NAME}"

# 2. PkgInfo
echo -n "APPL????" > "${APP_DIR}/Contents/PkgInfo"

# 3. Info.plist
cat << 'PLIST' > "${APP_DIR}/Contents/Info.plist"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>CFBundleDevelopmentRegion</key>
	<string>en</string>
	<key>CFBundleExecutable</key>
	<string>Banglish</string>
	<key>CFBundleIconFile</key>
	<string>AppIcon</string>
	<key>CFBundleIconName</key>
	<string>AppIcon</string>
	<key>CFBundleIdentifier</key>
	<string>org.banglish.inputmethod.Banglish</string>
	<key>CFBundleInfoDictionaryVersion</key>
	<string>6.0</string>
	<key>CFBundleName</key>
	<string>Banglish</string>
	<key>CFBundleDisplayName</key>
	<string>Banglish</string>
	<key>CFBundlePackageType</key>
	<string>APPL</string>
	<key>CFBundleShortVersionString</key>
	<string>1.0.001</string>
	<key>CFBundleSignature</key>
	<string>????</string>
	<key>CFBundleSupportedPlatforms</key>
	<array>
		<string>MacOSX</string>
	</array>
	<key>CFBundleVersion</key>
	<string>1</string>
	<key>InputMethodConnectionName</key>
	<string>org.banglish.inputmethod.Banglish_Connection</string>
	<key>InputMethodServerControllerClass</key>
	<string>BanglishInputController</string>
	<key>LSMinimumSystemVersion</key>
	<string>12.0</string>
	<key>LSUIElement</key>
	<true/>
	<key>NSPrincipalClass</key>
	<string>NSApplication</string>
	<key>NSSupportsSuddenTermination</key>
	<true/>
	<key>TISIconIsTemplate</key>
	<false/>
	<key>tsInputMethodCharacterRepertoireKey</key>
	<array>
		<string>Beng</string>
	</array>
	<key>tsInputMethodIconFileKey</key>
	<string>MenuIcon</string>
</dict>
</plist>
PLIST

# 4. InfoPlist.strings (English and Bangla)
mkdir -p "${APP_DIR}/Contents/Resources/English.lproj"
mkdir -p "${APP_DIR}/Contents/Resources/bn.lproj"

cat << 'STRINGS' > "${APP_DIR}/Contents/Resources/English.lproj/InfoPlist.strings"
"CFBundleName" = "Banglish";
"CFBundleDisplayName" = "Banglish";
"CFBundleShortVersionString" = "Banglish version 1.0.001";
"CFBundleGetInfoString" = "Banglish 1.0.001, Bangla Phonetic Keyboard with Word Suggestions";
"NSHumanReadableCopyright" = "Copyright © 2026 Banglish";
STRINGS

cat << 'STRINGS' > "${APP_DIR}/Contents/Resources/bn.lproj/InfoPlist.strings"
"CFBundleName" = "বাংলিশ";
"CFBundleDisplayName" = "বাংলিশ (Banglish)";
"CFBundleShortVersionString" = "বাংলিশ সংস্করণ ১.০.০০১";
"CFBundleGetInfoString" = "বাংলিশ ১.০.০০১, বাংলা ফোনেটিক কীবোর্ড ও শব্দ সাজেশন";
"NSHumanReadableCopyright" = "কপিরাইট © ২০২৬ বাংলিশ";
STRINGS

# 5. Resources (icons & words dictionary)
cp "${PROJECT_DIR}/Resources/AppIcon.icns" "${APP_DIR}/Contents/Resources/"
cp "${PROJECT_DIR}/Resources/MenuIcon.png" "${APP_DIR}/Contents/Resources/"
cp "${PROJECT_DIR}/Resources/MenuIcon.tiff" "${APP_DIR}/Contents/Resources/"
if [ -f "${PROJECT_DIR}/Resources/words.txt" ]; then
    cp "${PROJECT_DIR}/Resources/words.txt" "${APP_DIR}/Contents/Resources/"
fi

# 6. Codesign ad-hoc & strip xattr
xattr -cr "${APP_DIR}"
codesign --force --deep --sign - "${APP_DIR}"

# 7. Build PKG
echo "-> Building PKG Installer..."
mkdir -p "${PKG_DIR}/scripts"
cp -R "${APP_DIR}" "${PKG_DIR}/scripts/${APP_NAME}.app"

cat << 'SCRIPT' > "${PKG_DIR}/scripts/preinstall"
#!/bin/bash
REAL_USER=$(stat -f "%Su" /dev/console 2>/dev/null || echo "$USER")
REAL_HOME=$(eval echo "~$REAL_USER")

killall Banglish 2>/dev/null || true
sleep 0.5

rm -rf "$REAL_HOME/Library/Input Methods/Banglish.app" 2>/dev/null || true
rm -rf "/Library/Input Methods/Banglish.app" 2>/dev/null || true
rm -rf "/Library/Input Methods/Banglish.localized" 2>/dev/null || true
exit 0
SCRIPT
chmod +x "${PKG_DIR}/scripts/preinstall"

cat << 'SCRIPT' > "${PKG_DIR}/scripts/postinstall"
#!/bin/bash
REAL_USER=$(stat -f "%Su" /dev/console 2>/dev/null || echo "$USER")
REAL_HOME=$(eval echo "~$REAL_USER")

INSTALL_DIR="$REAL_HOME/Library/Input Methods"
SCRIPT_DIR="$(dirname "$0")"

mkdir -p "$INSTALL_DIR"
cp -R "$SCRIPT_DIR/Banglish.app" "$INSTALL_DIR/"
chown -R "$REAL_USER" "$INSTALL_DIR/Banglish.app"
xattr -cr "$INSTALL_DIR/Banglish.app" 2>/dev/null || true

rm -f "/Applications/Banglish.app" 2>/dev/null || true
rm -rf "/Applications/Banglish.app" 2>/dev/null || true
ln -sf "$INSTALL_DIR/Banglish.app" "/Applications/Banglish.app"

killall Banglish 2>/dev/null || true
sleep 0.5
su "$REAL_USER" -c "open '$INSTALL_DIR/Banglish.app'" 2>/dev/null || true

sleep 2
pkill -x -u "$REAL_USER" TextInputMenuAgent 2>/dev/null || true
pkill -KILL -x -u "$REAL_USER" TextInputSwitcher 2>/dev/null || true
pkill -KILL -x -u "$REAL_USER" CursorUIViewService 2>/dev/null || true
exit 0
SCRIPT
chmod +x "${PKG_DIR}/scripts/postinstall"

pkgbuild --nopayload \
         --scripts "${PKG_DIR}/scripts" \
         --identifier org.banglish.inputmethod.Banglish \
         --version 1.0 \
         "${PKG_OUTPUT}"

# 8. Build DMG
echo "-> Packaging DMG..."
mkdir -p "${DMG_STAGING}"
cp "${PKG_OUTPUT}" "${DMG_STAGING}/Install Banglish.pkg"
cp -R "${APP_DIR}" "${DMG_STAGING}/Banglish.app"

cat << 'EOF' > "${DMG_STAGING}/ইনস্টল করার নিয়ম (README).txt"
============================================================
              Banglish - বাংলা ফনেটিক কীবোর্ড (macOS)
============================================================

ইনস্টল করার সহজ নিয়ম:
১. "Install Banglish.pkg"-এ ডাবল ক্লিক করে ইনস্টল সম্পন্ন করুন।
২. System Settings খুলুন -> Keyboard -> Text Input (বা Input Sources)-এ Edit চাপুন।
৩. (+) বাটনে ক্লিক করে "Bangla" সিলেক্ট করে "Banglish" অ্যাড করুন।
৪. কীবোর্ড শর্টকাট (Ctrl + Space বা Globe কী) দিয়ে Banglish সিলেক্ট করে টাইপ শুরু করুন!

------------------------------------------------------------
SECURITY GUIDE (গেটকিপার সিকিউরিটি অনুমোদন):
ম্যাকের ডিফল্ট সিকিউরিটির (Gatekeeper) কারণে যদি অ্যাপ সরাসরি ওপেন হতে বাধা দেয়,
তাহলে খুব সহজেই সিস্টেম সেটিংস থেকে অনুমোদন দিতে পারেন:

System Settings ➔ Privacy & Security ➔ Allow applications downloaded from ➔ Open Anyway

Privacy & Security ট্যাবে গিয়ে "Allow applications downloaded from" সেকশনের
পাশে থাকা "Open Anyway" বাটনে ক্লিক করলেই অ্যাপটি সফলভাবে চালু হয়ে যাবে।
------------------------------------------------------------

Developer Credits:
FramEmpire (www.framempire.com) & A M Pabel
============================================================
EOF

rm -f "${DMG_OUTPUT}"
hdiutil create \
    -volname "Banglish" \
    -srcfolder "${DMG_STAGING}" \
    -ov \
    -format UDZO \
    "${DMG_OUTPUT}"

cp "${DMG_OUTPUT}" "${PROJECT_DIR}/${APP_NAME}.dmg"
cp "${PKG_OUTPUT}" "${PROJECT_DIR}/${APP_NAME}.pkg"
cp "${DMG_OUTPUT}" "${PROJECT_DIR}/${APP_NAME}_Installer.dmg"
cp "${PKG_OUTPUT}" "${PROJECT_DIR}/${APP_NAME}_Installer.pkg"


echo "=========================================="
echo " BUILD SUCCESS!"
echo " Versioned DMG: ${DMG_OUTPUT}"
echo " Versioned PKG: ${PKG_OUTPUT}"
echo "=========================================="

