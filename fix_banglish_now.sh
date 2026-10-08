#!/bin/bash
set -e

echo "=== Banglish Setup & Refresh ==="

# Step 1: Kill old processes
killall Banglish 2>/dev/null || true
sleep 0.5

# Step 2: Remove old root and user copies
rm -rf "/Library/Input Methods/Banglish.app" 2>/dev/null || true
rm -rf "/Library/Input Methods/Banglish.localized" 2>/dev/null || true
rm -rf "$HOME/Library/Input Methods/Banglish.app" 2>/dev/null || true
rm -rf "/Applications/Banglish.app" 2>/dev/null || true

# Step 2.5: Build fresh release bundle and installer DMG
bash "/Users/rmacstudio2/Documents/Banglish/scripts/build_release_dmg.sh"

# Step 3: Install verified build to ~/Library/Input Methods (and /Library if root)
mkdir -p "$HOME/Library/Input Methods"
rm -rf "$HOME/Library/Input Methods/Banglish.app" 2>/dev/null || true
cp -R "/Users/rmacstudio2/Documents/Banglish/build/Banglish.app" "$HOME/Library/Input Methods/"
chown -R "$(whoami)" "$HOME/Library/Input Methods/Banglish.app" 2>/dev/null || true
chmod -R 755 "$HOME/Library/Input Methods/Banglish.app"
xattr -cr "$HOME/Library/Input Methods/Banglish.app"

if [ -w "/Library/Input Methods" ]; then
    rm -rf "/Library/Input Methods/Banglish.app" 2>/dev/null || true
    cp -R "/Users/rmacstudio2/Documents/Banglish/build/Banglish.app" "/Library/Input Methods/"
    chmod -R 755 "/Library/Input Methods/Banglish.app"
    xattr -cr "/Library/Input Methods/Banglish.app"
fi

# Step 4: Symlink to /Applications
ln -sf "$HOME/Library/Input Methods/Banglish.app" "/Applications/Banglish.app"

# Step 5: Register with LaunchServices
/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister \
    -f -r "$HOME/Library/Input Methods/Banglish.app"
if [ -d "/Library/Input Methods/Banglish.app" ]; then
    /System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister \
        -f -r "/Library/Input Methods/Banglish.app"
fi

# Step 6: Register with TIS
/Library/Developer/CommandLineTools/usr/bin/swift -sdk /Library/Developer/CommandLineTools/SDKs/MacOSX.sdk -e '
import Carbon
import Foundation
let appPath = "\(NSHomeDirectory())/Library/Input Methods/Banglish.app"
let url = URL(fileURLWithPath: appPath) as CFURL
let err = TISRegisterInputSource(url)
print("  TISRegisterInputSource result: \(err)")

let allSources = TISCreateInputSourceList(nil, true)!.takeRetainedValue() as! [TISInputSource]
for s in allSources {
    let idPtr = TISGetInputSourceProperty(s, kTISPropertyInputSourceID)
    if let idPtr = idPtr {
        let id = Unmanaged<CFString>.fromOpaque(idPtr).takeUnretainedValue() as String
        if id == "org.banglish.inputmethod.Banglish" {
            let enableErr = TISEnableInputSource(s)
            let selectErr = TISSelectInputSource(s)
            print("  TISEnableInputSource: \(enableErr), Select: \(selectErr)")
        }
    }
}
'

# Step 7: Launch as user and restart daemons
open "$HOME/Library/Input Methods/Banglish.app" 2>/dev/null || true

sleep 1
pkill -x -u rmacstudio2 TextInputMenuAgent 2>/dev/null || true
pkill -KILL -x -u rmacstudio2 TextInputSwitcher 2>/dev/null || true
pkill -KILL -x -u rmacstudio2 CursorUIViewService 2>/dev/null || true

echo ""
echo "=========================================================="
echo " SUCCESS! Banglish registered under Bangla (bn)."
echo " Now open System Settings -> Keyboard -> Text Input (Edit...)"
echo " Click (+) -> Select 'Bangla' -> Select 'Banglish' -> Add"
echo "=========================================================="
