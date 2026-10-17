#!/usr/bin/env bash
set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP_NAME="Banglish"
VERSION="1.0.002-beta"
BUILD_DIR="${PROJECT_DIR}/build"
APP_DIR="${BUILD_DIR}/${APP_NAME}.app"
PKG_DIR="${PROJECT_DIR}/build/pkg_root"
DMG_STAGING="${PROJECT_DIR}/build/dmg_release"
DMG_OUTPUT="${PROJECT_DIR}/${APP_NAME}-v${VERSION}.dmg"
PKG_OUTPUT="${PROJECT_DIR}/${APP_NAME}-v${VERSION}.pkg"

echo "=========================================="
echo " Building ${APP_NAME} v${VERSION} Beta DMG"
echo "=========================================="

rm -rf "${BUILD_DIR}"
mkdir -p "${APP_DIR}/Contents/MacOS"
mkdir -p "${APP_DIR}/Contents/Resources/English.lproj"
mkdir -p "${APP_DIR}/Contents/Resources/bn.lproj"
mkdir -p "${APP_DIR}/Contents/Resources/ar.lproj"

# 1. Compile native Swift binary
echo "-> Compiling Swift sources for v${VERSION}..."
SDK_PATH="/Library/Developer/CommandLineTools/SDKs/MacOSX.sdk"
mkdir -p "${BUILD_DIR}/ModuleCache"
/Library/Developer/CommandLineTools/usr/bin/swiftc \
    -O \
    -sdk "${SDK_PATH}" \
    -target arm64-apple-macos12.0 \
    -module-cache-path "${BUILD_DIR}/ModuleCache" \
    -framework Cocoa \
    -framework InputMethodKit \
    "${PROJECT_DIR}/src/BanglishData.swift" \
    "${PROJECT_DIR}/src/BanglishEngine.swift" \
    "${PROJECT_DIR}/src/BanglishDictionary.swift" \
    "${PROJECT_DIR}/src/ArabicData.swift" \
    "${PROJECT_DIR}/src/ArabicEngine.swift" \
    "${PROJECT_DIR}/src/ArabicDictionary.swift" \
    "${PROJECT_DIR}/src/CandidateWindow.swift" \
    "${PROJECT_DIR}/src/BanglishInputController.swift" \
    "${PROJECT_DIR}/src/AppUI.swift" \
    "${PROJECT_DIR}/src/main.swift" \
    -o "${APP_DIR}/Contents/MacOS/${APP_NAME}"

# 2. PkgInfo
echo -n "APPL????" > "${APP_DIR}/Contents/PkgInfo"

# 3. Info.plist with dual input modes: Banglish & Banglish AR
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
	<string>1.0.002-beta</string>
	<key>CFBundleSignature</key>
	<string>????</string>
	<key>CFBundleSupportedPlatforms</key>
	<array>
		<string>MacOSX</string>
	</array>
	<key>CFBundleVersion</key>
	<string>2</string>
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
		<string>Arab</string>
	</array>
	<key>tsInputMethodIconFileKey</key>
	<string>MenuIcon</string>
	<key>ComponentInputModeDict</key>
	<dict>
		<key>tsInputModeListKey</key>
		<dict>
			<key>org.banglish.inputmethod.Banglish.Bangla</key>
			<dict>
				<key>tsInputModeCharacterRepertoireKey</key>
				<array>
					<string>Beng</string>
				</array>
				<key>tsInputModeDefaultStateKey</key>
				<true/>
				<key>tsInputModeIsVisibleKey</key>
				<true/>
				<key>tsInputModeKeyEquivalentModifiersKey</key>
				<integer>0</integer>
				<key>tsInputModeMenuIconFileKey</key>
				<string>MenuIcon</string>
				<key>tsInputModePaletteIconFileKey</key>
				<string>MenuIcon</string>
				<key>tsInputModePrimaryInScriptKey</key>
				<true/>
				<key>tsInputModeScriptKey</key>
				<string>smBengali</string>
			</dict>
			<key>org.banglish.inputmethod.Banglish.Arabic</key>
			<dict>
				<key>tsInputModeCharacterRepertoireKey</key>
				<array>
					<string>Arab</string>
				</array>
				<key>tsInputModeDefaultStateKey</key>
				<true/>
				<key>tsInputModeIsVisibleKey</key>
				<true/>
				<key>tsInputModeKeyEquivalentModifiersKey</key>
				<integer>0</integer>
				<key>tsInputModeMenuIconFileKey</key>
				<string>MenuIconAR</string>
				<key>tsInputModePaletteIconFileKey</key>
				<string>MenuIconAR</string>
				<key>tsInputModePrimaryInScriptKey</key>
				<true/>
				<key>tsInputModeScriptKey</key>
				<string>smArabic</string>
			</dict>
		</dict>
	</dict>
</dict>
</plist>
PLIST

# 4. InfoPlist.strings (English, Bangla, Arabic)
cat << 'STRINGS' > "${APP_DIR}/Contents/Resources/English.lproj/InfoPlist.strings"
"CFBundleName" = "Banglish";
"CFBundleDisplayName" = "Banglish";
"org.banglish.inputmethod.Banglish.Bangla" = "Banglish";
"org.banglish.inputmethod.Banglish.Arabic" = "Banglish AR";
"CFBundleShortVersionString" = "Banglish version 1.0.002-beta";
"CFBundleGetInfoString" = "Banglish 1.0.002 Beta, Bangla & Arabic Phonetic Keyboard";
"NSHumanReadableCopyright" = "Copyright © 2026 Banglish";
STRINGS

cat << 'STRINGS' > "${APP_DIR}/Contents/Resources/bn.lproj/InfoPlist.strings"
"CFBundleName" = "বাংলিশ";
"CFBundleDisplayName" = "বাংলিশ (Banglish)";
"org.banglish.inputmethod.Banglish.Bangla" = "বাংলিশ";
"org.banglish.inputmethod.Banglish.Arabic" = "Banglish AR";
"CFBundleShortVersionString" = "বাংলিশ সংস্করণ ১.০.০০২-বিটা";
"CFBundleGetInfoString" = "বাংলিশ ১.০.০০২ বিটা, বাংলা ও আরবি ফোনেটিক কীবোর্ড";
"NSHumanReadableCopyright" = "কপিরাইট © ২০২৬ বাংলিশ";
STRINGS

cat << 'STRINGS' > "${APP_DIR}/Contents/Resources/ar.lproj/InfoPlist.strings"
"CFBundleName" = "بانجليش";
"CFBundleDisplayName" = "Banglish AR";
"org.banglish.inputmethod.Banglish.Bangla" = "Banglish";
"org.banglish.inputmethod.Banglish.Arabic" = "Banglish AR";
"CFBundleShortVersionString" = "Banglish 1.0.002-beta";
"CFBundleGetInfoString" = "Banglish 1.0.002 Beta, لوحة مفاتيح صوتية للبنغالية والعربية";
"NSHumanReadableCopyright" = "حقوق النشر © 2026 Banglish";
STRINGS

# 5. Resources (icons & words dictionary)
cp "${PROJECT_DIR}/Resources/AppIcon.icns" "${APP_DIR}/Contents/Resources/"
cp "${PROJECT_DIR}/Resources/MenuIcon.png" "${APP_DIR}/Contents/Resources/"
cp "${PROJECT_DIR}/Resources/MenuIcon.tiff" "${APP_DIR}/Contents/Resources/"
cp "${PROJECT_DIR}/Resources/MenuIconAR.png" "${APP_DIR}/Contents/Resources/"
cp "${PROJECT_DIR}/Resources/MenuIconAR.tiff" "${APP_DIR}/Contents/Resources/"
if [ -f "${PROJECT_DIR}/Resources/words.txt" ]; then
    cp "${PROJECT_DIR}/Resources/words.txt" "${APP_DIR}/Contents/Resources/"
fi
if [ -f "${PROJECT_DIR}/Resources/autodict.txt" ]; then
    cp "${PROJECT_DIR}/Resources/autodict.txt" "${APP_DIR}/Contents/Resources/"
fi

# 6. Codesign ad-hoc & strip xattr
xattr -cr "${APP_DIR}"
codesign --force --deep --sign - "${APP_DIR}"

# 7. Build PKG Installer
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
         --version 1.0.002-beta \
         "${PKG_OUTPUT}"

# 8. Build DMG
echo "-> Packaging DMG..."
mkdir -p "${DMG_STAGING}"
cp "${PKG_OUTPUT}" "${DMG_STAGING}/Install Banglish.pkg"
cp -R "${APP_DIR}" "${DMG_STAGING}/Banglish.app"

cat << 'EOF' > "${DMG_STAGING}/ইনস্টল ও ব্যবহার করার নিয়ম.txt"
============================================================
       Banglish v1.0.002 Beta - বাংলা ও আরবি কীবোর্ড
============================================================

নতুন ফিচার:
১. বাংলা ফোনেটিক (Banglish)
২. আরবি ফোনেটিক (Banglish AR / Arabizi)

ইনস্টল করার নিয়ম:
১. "Install Banglish.pkg"-এ ডাবল ক্লিক করে ইনস্টল সম্পন্ন করুন।
২. System Settings ➔ Keyboard ➔ Text Input (Input Sources)-এ Edit চাপুন।
৩. (+) বাটনে ক্লিক করে:
   - "Bangla" তালিকা থেকে "Banglish" অ্যাড করুন।
   - "Arabic" তালিকা থেকে "Banglish AR" অ্যাড করুন।

কীবোর্ড শর্টকাট:
- বাংলা <-> ইংলিশ: Option + Space (⌥ + Space)
- আরবি <-> ইংলিশ: Control + Option + Space (^ + ⌥ + Space)
- ম্যাকের Dedicated Fn/Globe (🌐) কী ট্যাপ করলেও ভাষা পরিবর্তন হবে।

============================================================
EOF

rm -f "${DMG_OUTPUT}"
hdiutil create \
    -volname "Banglish-Beta" \
    -srcfolder "${DMG_STAGING}" \
    -ov \
    -format UDZO \
    "${DMG_OUTPUT}"

echo "=========================================="
echo " BUILD SUCCESS!"
echo " Versioned Beta DMG: ${DMG_OUTPUT}"
echo " Versioned Beta PKG: ${PKG_OUTPUT}"
echo "=========================================="
