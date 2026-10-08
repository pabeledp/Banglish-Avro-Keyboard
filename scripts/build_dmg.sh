#!/usr/bin/env bash
set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP_NAME="Banglish"
BUILD_DIR="${PROJECT_DIR}/build"
APP_BUNDLE="${BUILD_DIR}/${APP_NAME}.app"
DMG_STAGING="${BUILD_DIR}/dmg_staging"
DMG_OUTPUT="${PROJECT_DIR}/${APP_NAME}_Installer.dmg"

echo "Building regular Mac DMG Installer (Drag to Applications)..."
rm -rf "${DMG_STAGING}" "${DMG_OUTPUT}"
mkdir -p "${DMG_STAGING}"

cp -R "${APP_BUNDLE}" "${DMG_STAGING}/"
ln -s "/Applications" "${DMG_STAGING}/Applications"

hdiutil create \
    -volname "${APP_NAME}" \
    -srcfolder "${DMG_STAGING}" \
    -ov \
    -format UDZO \
    "${DMG_OUTPUT}"

echo "SUCCESS! Created user-friendly Applications DMG at: ${DMG_OUTPUT}"
