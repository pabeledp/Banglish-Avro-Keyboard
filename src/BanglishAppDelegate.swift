import Cocoa
import InputMethodKit

@objc(BanglishAppDelegate)
public final class BanglishAppDelegate: NSObject, NSApplicationDelegate {
    private var statusItem: NSStatusItem?
    public static var isBanglaMode: Bool = true {
        didSet {
            NotificationCenter.default.post(name: .banglishModeChanged, object: nil)
        }
    }

    public func applicationDidFinishLaunching(_ notification: Notification) {
        setupStatusBar()
    }

    private func setupStatusBar() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        updateStatusItemTitle()

        let menu = NSMenu()
        let toggleItem = NSMenuItem(title: "Toggle Bangla / English (⌥ + Space)", action: #selector(toggleMode), keyEquivalent: "")
        toggleItem.target = self
        menu.addItem(toggleItem)

        menu.addItem(NSMenuItem.separator())

        let aboutItem = NSMenuItem(title: "About Banglish", action: #selector(showAbout), keyEquivalent: "")
        aboutItem.target = self
        menu.addItem(aboutItem)

        let quitItem = NSMenuItem(title: "Quit Banglish", action: #selector(quitApp), keyEquivalent: "q")
        quitItem.target = self
        menu.addItem(quitItem)

        statusItem?.menu = menu

        NotificationCenter.default.addObserver(
            self,
            selector: #selector(updateStatusItemTitle),
            name: .banglishModeChanged,
            object: nil
        )
    }

    @objc public func toggleMode() {
        BanglishAppDelegate.isBanglaMode.toggle()
        updateStatusItemTitle()
    }

    @objc private func updateStatusItemTitle() {
        DispatchQueue.main.async { [weak self] in
            guard let button = self?.statusItem?.button else { return }
            button.title = BanglishAppDelegate.isBanglaMode ? "বা" : "EN"
            button.toolTip = BanglishAppDelegate.isBanglaMode ? "Banglish: Bangla Mode" : "Banglish: English Passthrough"
        }
    }

    @objc private func showAbout() {
        let alert = NSAlert()
        alert.messageText = "Banglish - Bangla Phonetic IME"
        alert.informativeText = "Native macOS Input Method Editor for Bangla phonetic typing.\nVersion 1.0.0"
        alert.alertStyle = .informational
        alert.runModal()
    }

    @objc private func quitApp() {
        NSApplication.shared.terminate(nil)
    }
}

extension Notification.Name {
    static let banglishModeChanged = Notification.Name("BanglishModeChangedNotification")
}
