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
        CandidateWindow.shared.hide()
        _ = currentClient(sender: sender)
        buffer.removeAll()
    }

    override func deactivateServer(_ sender: Any!) {
        CandidateWindow.shared.hide()
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

        // Number keys 1...9 to select candidate
        let numberKeyCodes: [UInt16: Int] = [
            18: 0, // 1
            19: 1, // 2
            20: 2, // 3
            21: 3, // 4
            23: 4, // 5
            22: 5, // 6
            26: 6, // 7
            28: 7, // 8
            25: 8  // 9
        ]
        if CandidateWindow.shared.isCandidateWindowVisible,
           !modifiers.contains(.command), !modifiers.contains(.control), !modifiers.contains(.option),
           let idx = numberKeyCodes[keyCode] {
            CandidateWindow.shared.selectIndex(idx)
            return true
        }

        // Up Arrow (keycode 126): Move candidate selection up
        if keyCode == 126 && CandidateWindow.shared.isCandidateWindowVisible {
            CandidateWindow.shared.selectPrevious()
            return true
        }

        // Down Arrow (keycode 125): Move candidate selection down
        if keyCode == 125 && CandidateWindow.shared.isCandidateWindowVisible {
            CandidateWindow.shared.selectNext()
            return true
        }

        // Tab (keycode 48): Cycle candidates if popup is open
        if keyCode == 48 {
            if CandidateWindow.shared.isCandidateWindowVisible {
                CandidateWindow.shared.selectNext()
                return true
            }
            if !buffer.isEmpty { commitBuffer(client: client) }
            return false
        }

        // Space (keycode 49): commit selected candidate, let space pass through
        if keyCode == 49 {
            if !buffer.isEmpty {
                commitBuffer(client: client)
            }
            return false
        }

        // Return (keycode 36 or 76): commit selected candidate directly
        if keyCode == 36 || keyCode == 76 {
            if !buffer.isEmpty {
                commitBuffer(client: client)
                return true
            }
            return false
        }

        // Backspace (keycode 51)
        if keyCode == 51 {
            if !buffer.isEmpty {
                buffer.removeLast()
                if buffer.isEmpty {
                    CandidateWindow.shared.hide()
                    clearMarkedText(client: client)
                } else {
                    updateMarkedText(client: client)
                    updateCandidates(client: client)
                }
                return true
            }
            return false
        }

        // Escape (keycode 53)
        if keyCode == 53 {
            if CandidateWindow.shared.isCandidateWindowVisible {
                CandidateWindow.shared.hide()
                return true
            }
            if !buffer.isEmpty {
                buffer.removeAll()
                clearMarkedText(client: client)
                return true
            }
            return false
        }

        // Left / Right Arrows (123, 124)
        if keyCode == 123 || keyCode == 124 {
            if !buffer.isEmpty { commitBuffer(client: client) }
            return false
        }

        // Full stop / Daari (.)
        if char == "." {
            if !buffer.isEmpty {
                let chosen = CandidateWindow.shared.selectedCandidate() ?? engine.transliterate(buffer)
                client.insertText((chosen + "।") as NSString, replacementRange: Self.emptyRange)
                buffer.removeAll()
                CandidateWindow.shared.hide()
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
        updateCandidates(client: client)
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
        CandidateWindow.shared.hide()
        let marked = client.markedRange()
        let replaceRange = (marked.location != NSNotFound && marked.length > 0) ? marked : Self.emptyRange
        client.setMarkedText(
            "" as NSString,
            selectionRange: NSRange(location: 0, length: 0),
            replacementRange: replaceRange
        )
    }

    private func updateCandidates(client: any IMKTextInput) {
        guard !buffer.isEmpty else {
            CandidateWindow.shared.hide()
            return
        }

        let candidates = BanglishDictionary.shared.candidates(for: buffer)
        let rect = cursorRect(for: client)

        CandidateWindow.shared.show(candidates: candidates, near: rect) { [weak self, weak client] chosenCandidate in
            guard let self = self, let client = client else { return }
            self.commitText(chosenCandidate, client: client)
        }
    }

    private func cursorRect(for client: any IMKTextInput) -> NSRect {
        var rect = NSRect.zero
        var actualRange = NSRange(location: NSNotFound, length: 0)

        // 1. Try marked range insertion point and bounding box
        let marked = client.markedRange()
        if marked.location != NSNotFound && marked.length > 0 {
            // Insertion point at end of marked text
            rect = client.firstRect(forCharacterRange: NSRange(location: marked.location + marked.length, length: 0), actualRange: &actualRange)
            if isValidCursorRect(rect) { return rect }

            // Full marked range
            rect = client.firstRect(forCharacterRange: marked, actualRange: &actualRange)
            if isValidCursorRect(rect) { return rect }

            // Last character of marked text
            rect = client.firstRect(forCharacterRange: NSRange(location: marked.location + marked.length - 1, length: 1), actualRange: &actualRange)
            if isValidCursorRect(rect) { return rect }
        }

        // 2. Try selected range (current caret / cursor position in any text box)
        let selected = client.selectedRange()
        if selected.location != NSNotFound {
            rect = client.firstRect(forCharacterRange: selected, actualRange: &actualRange)
            if isValidCursorRect(rect) { return rect }
        }

        // 3. Try insertion point (NSNotFound, 0)
        rect = client.firstRect(forCharacterRange: NSRange(location: NSNotFound, length: 0), actualRange: &actualRange)
        if isValidCursorRect(rect) { return rect }

        // 4. Try lineHeightRectangle from attributes
        var lineRect = NSRect.zero
        _ = client.attributes(forCharacterIndex: 0, lineHeightRectangle: &lineRect)
        if isValidCursorRect(lineRect) { return lineRect }

        if marked.location != NSNotFound {
            _ = client.attributes(forCharacterIndex: Int(marked.location), lineHeightRectangle: &lineRect)
            if isValidCursorRect(lineRect) { return lineRect }
        }

        return NSRect.zero
    }

    private func isValidCursorRect(_ rect: NSRect) -> Bool {
        guard rect.origin.x > 10 && rect.origin.y > 10 else { return false }
        guard rect.width > 0 || rect.height > 0 else { return false }
        guard NSScreen.screens.contains(where: { NSPointInRect(rect.origin, $0.frame) }) else { return false }
        return true
    }

    private func commitBuffer(client: any IMKTextInput) {
        guard !buffer.isEmpty else { return }
        let textToCommit = CandidateWindow.shared.selectedCandidate() ?? engine.transliterate(buffer)
        commitText(textToCommit, client: client)
    }

    private func commitText(_ text: String, client: any IMKTextInput) {
        let marked = client.markedRange()
        let replaceRange = (marked.location != NSNotFound && marked.length > 0) ? marked : Self.emptyRange
        client.insertText(text as NSString, replacementRange: replaceRange)
        buffer.removeAll()
        CandidateWindow.shared.hide()
    }

    // MARK: - Menu Bar
    override func menu() -> NSMenu! {
        let menu = NSMenu()
        let item1 = NSMenuItem(title: "🇧🇩 Banglish 1.0.001", action: nil, keyEquivalent: "")
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
