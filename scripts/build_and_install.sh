#!/usr/bin/env bash
set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP_NAME="Banglish"
APP_BUNDLE="${PROJECT_DIR}/build/${APP_NAME}.app"
CONTENTS_DIR="${APP_BUNDLE}/Contents"
MACOS_DIR="${CONTENTS_DIR}/MacOS"
RESOURCES_DIR="${CONTENTS_DIR}/Resources"
TARGET_DIR="${HOME}/Library/Input Methods"

echo "=========================================="
echo " Building ${APP_NAME} for macOS"
echo "=========================================="

# 1. Clean previous build
rm -rf "${PROJECT_DIR}/build"
mkdir -p "${MACOS_DIR}"
mkdir -p "${RESOURCES_DIR}"

# 2. Compile Swift sources
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
    "${PROJECT_DIR}/src/BanglishAppDelegate.swift" \
    "${PROJECT_DIR}/src/BanglishInputController.swift" \
    "${PROJECT_DIR}/src/main.swift" \
    -o "${MACOS_DIR}/${APP_NAME}"

# 3. Copy Resources & Info.plist
echo "-> Copying Info.plist and resources..."
cp "${PROJECT_DIR}/Resources/Info.plist" "${CONTENTS_DIR}/Info.plist"
cp -R "${PROJECT_DIR}/Resources/"* "${RESOURCES_DIR}/"

echo "=========================================="
echo " Installing ${APP_NAME} to ${TARGET_DIR}"
echo "=========================================="

mkdir -p "${TARGET_DIR}"

# Remove existing installed app
if [ -d "${TARGET_DIR}/${APP_NAME}.app" ]; then
    echo "-> Removing existing version from Input Methods..."
    rm -rf "${TARGET_DIR}/${APP_NAME}.app"
fi

# Copy new build
echo "-> Copying ${APP_NAME}.app to ${TARGET_DIR}..."
cp -R "${APP_BUNDLE}" "${TARGET_DIR}/"

# Re-register with LaunchServices
echo "-> Registering with macOS LaunchServices..."
/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -f "${TARGET_DIR}/${APP_NAME}.app" || true

# Terminate running instances to reload
echo "-> Restarting any running ${APP_NAME} instances..."
killall -9 "${APP_NAME}" 2>/dev/null || true

echo "=========================================="
echo " Installation Complete!"
echo "=========================================="
echo ""
echo "Next steps to activate Banglish on your Mac:"
echo "1. Open 'System Settings' (or 'System Preferences')."
echo "2. Go to 'Keyboard' -> 'Text Input' -> Click 'Edit...' next to Input Sources."
echo "3. Click '+' at the bottom left."
echo "4. In the language list, select 'Bangla' (or 'Bengali') or look for 'Banglish'."
echo "5. Add 'Banglish' and select it."
echo "6. Press Option + Space (⌥ + Space) to toggle between Bangla & English typing."
echo "=========================================="
