import Cocoa
import InputMethodKit

@objc(BanglishInputController)
class BanglishInputController: IMKInputController {
    private var buffer = ""
    private let engine = BanglishEngine.shared
    private static let emptyRange = NSRange(location: NSNotFound, length: 0)
    public static var isBanglaMode = true
    private weak var lastClient: (any IMKTextInput)?

    override init!(server: IMKServer!, delegate: Any!, client: Any!) {
        super.init(server: server, delegate: delegate, client: client)
        if let c = client as? (any IMKTextInput) {
            self.lastClient = c
        }
    }

    private func currentClient(sender: Any? = nil) -> (any IMKTextInput)? {
        if let client = sender as? (any IMKTextInput) {
            lastClient = client
            return client
        }
        if let client = self.client() as? (any IMKTextInput) {
            lastClient = client
            return client
        }
        return lastClient
    }

    // MARK: - Server Lifecycle
    override func recognizedEvents(_ sender: Any!) -> Int {
        return Int(NSEvent.EventTypeMask.keyDown.rawValue)
    }

    override func activateServer(_ sender: Any!) {
        super.activateServer(sender)
        _ = currentClient(sender: sender)
        buffer.removeAll()
    }

    override func deactivateServer(_ sender: Any!) {
        if let client = currentClient(sender: sender), !buffer.isEmpty {
            commitBuffer(client: client)
        }
        super.deactivateServer(sender)
    }

    override func commitComposition(_ sender: Any!) {
        if let client = currentClient(sender: sender), !buffer.isEmpty {
            commitBuffer(client: client)
        }
    }

    // MARK: - Native Event Handling (Raw NSEvents)
    override func handle(_ event: NSEvent!, client sender: Any!) -> Bool {
        guard let event = event,
              event.type == .keyDown,
              let client = currentClient(sender: sender) else {
            return false
        }

        let modifiers = event.modifierFlags
        let keyCode = event.keyCode
        let char = event.characters?.first ?? event.charactersIgnoringModifiers?.first

        // Hotkey: Option + Space toggles Bangla/English
        if modifiers.contains(.option) && keyCode == 49 {
            if !buffer.isEmpty { commitBuffer(client: client) }
            BanglishInputController.isBanglaMode.toggle()
            return true
        }

        if !BanglishInputController.isBanglaMode {
            return false
        }

        // Pass through events with Cmd or Ctrl
        if modifiers.contains(.command) || modifiers.contains(.control) {
            if !buffer.isEmpty { commitBuffer(client: client) }
            return false
        }

        // Space (keycode 49): commit converted word, let space pass through
        if keyCode == 49 {
            if !buffer.isEmpty {
                commitBuffer(client: client)
            }
            return false
        }

        // Return (keycode 36 or 76): commit word, let newline pass through
        if keyCode == 36 || keyCode == 76 {
            if !buffer.isEmpty {
                commitBuffer(client: client)
            }
            return false
        }

        // Backspace (keycode 51)
        if keyCode == 51 {
            if !buffer.isEmpty {
                buffer.removeLast()
                updateMarkedText(client: client)
                return true
            }
            return false
        }

        // Escape (keycode 53)
        if keyCode == 53 {
            if !buffer.isEmpty {
                buffer.removeAll()
                clearMarkedText(client: client)
                return true
            }
            return false
        }

        // Tab (keycode 48)
        if keyCode == 48 {
            if !buffer.isEmpty { commitBuffer(client: client) }
            return false
        }

        // Arrows (123...126)
        if (123...126).contains(keyCode) {
            if !buffer.isEmpty { commitBuffer(client: client) }
            return false
        }

        // Full stop / Daari (.)
        if char == "." {
            if !buffer.isEmpty {
                let converted = engine.transliterate(buffer)
                client.insertText((converted + "।") as NSString, replacementRange: Self.emptyRange)
                buffer.removeAll()
                return true
            } else {
                client.insertText("।" as NSString, replacementRange: Self.emptyRange)
                return true
            }
        }

        // Regular printable ASCII characters
        guard let char = char, char.isASCII && !char.isNewline && char != " " else {
            return false
        }

        buffer.append(char)
        updateMarkedText(client: client)
        return true
    }

    // MARK: - Marked Text & Buffer Management
    private func updateMarkedText(client: any IMKTextInput) {
        if buffer.isEmpty {
            clearMarkedText(client: client)
            return
        }

        let converted = engine.transliterate(buffer)
        let attrString = NSMutableAttributedString(
            string: converted,
            attributes: [
                .underlineStyle: NSUnderlineStyle.single.rawValue,
                .font: NSFont.systemFont(ofSize: NSFont.systemFontSize)
            ]
        )

        client.setMarkedText(
            attrString,
            selectionRange: NSRange(location: converted.utf16.count, length: 0),
            replacementRange: Self.emptyRange
        )
    }

    private func clearMarkedText(client: any IMKTextInput) {
        let marked = client.markedRange()
        let replaceRange = (marked.location != NSNotFound && marked.length > 0) ? marked : Self.emptyRange
        client.setMarkedText(
            "" as NSString,
            selectionRange: NSRange(location: 0, length: 0),
            replacementRange: replaceRange
        )
    }

    private func commitBuffer(client: any IMKTextInput) {
        guard !buffer.isEmpty else { return }
        let converted = engine.transliterate(buffer)
        let marked = client.markedRange()
        let replaceRange = (marked.location != NSNotFound && marked.length > 0) ? marked : Self.emptyRange
        client.insertText(converted as NSString, replacementRange: replaceRange)
        buffer.removeAll()
    }

    // MARK: - Menu Bar
    override func menu() -> NSMenu! {
        let menu = NSMenu()
        let item1 = NSMenuItem(title: "🇧🇩 Banglish 1.0", action: nil, keyEquivalent: "")
        item1.isEnabled = false
        menu.addItem(item1)
        menu.addItem(NSMenuItem.separator())

        let toggle = NSMenuItem(title: BanglishInputController.isBanglaMode ? "Switch to English (⌥ + Space)" : "Switch to Bangla (⌥ + Space)", action: #selector(toggleModeMenu), keyEquivalent: "")
        toggle.target = self
        menu.addItem(toggle)

        menu.addItem(NSMenuItem.separator())
        let openSettings = NSMenuItem(title: "Open Banglish Dashboard...", action: #selector(openDashboard), keyEquivalent: "")
        openSettings.target = self
        menu.addItem(openSettings)

        let openMacSettings = NSMenuItem(title: "Open macOS Keyboard Settings...", action: #selector(openMacSettings), keyEquivalent: "")
        openMacSettings.target = self
        menu.addItem(openMacSettings)

        return menu
    }

    @objc func toggleModeMenu() {
        BanglishInputController.isBanglaMode.toggle()
    }

    @objc func openDashboard() {
        if let appDelegate = NSApplication.shared.delegate as? BanglishAppCoordinator {
            appDelegate.showMainWindow()
        }
    }

    @objc func openMacSettings() {
        if let url = URL(string: "x-apple.systempreferences:com.apple.Keyboard-Settings.extension") {
            NSWorkspace.shared.open(url)
        }
    }
}
