#!/bin/bash
set -e
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "Installing Banglish to /Library/Input Methods (Requires Admin Password)..."
sudo killall -9 Banglish 2>/dev/null || true
sudo rm -rf "/Library/Input Methods/Banglish.app"
sudo cp -R "${PROJECT_DIR}/build/Banglish.app" "/Library/Input Methods/"
sudo chown -R root:wheel "/Library/Input Methods/Banglish.app"
sudo chmod -R 755 "/Library/Input Methods/Banglish.app"

/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -f "/Library/Input Methods/Banglish.app"

echo "=================================================="
echo " SUCCESS! Banglish installed identically to Avro!"
echo "=================================================="
