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
        if let client = self.client() {
            lastClient = client
            return client
        }
        return lastClient
    }

    private static var fnKeyDownTime: TimeInterval = 0
    private static var fnUsedWithOtherKey = false

    // MARK: - Server Lifecycle
    override func recognizedEvents(_ sender: Any!) -> Int {
        return Int(NSEvent.EventTypeMask.keyDown.rawValue | NSEvent.EventTypeMask.flagsChanged.rawValue)
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
              let client = currentClient(sender: sender) else {
            return false
        }

        // 1. Dedicated Hardware Fn / Globe key (keycode 63) on Mac keyboards
        if event.type == .flagsChanged {
            if event.keyCode == 63 {
                let isFnDown = event.modifierFlags.contains(.function) ||
                               (event.modifierFlags.rawValue & NSEvent.ModifierFlags.function.rawValue) != 0
                if isFnDown {
                    Self.fnKeyDownTime = CACurrentMediaTime()
                    Self.fnUsedWithOtherKey = false
                } else {
                    let elapsed = CACurrentMediaTime() - Self.fnKeyDownTime
                    // Dedicated tap (released within 500ms without combination)
                    if !Self.fnUsedWithOtherKey && elapsed > 0.01 && elapsed < 0.6 {
                        if !buffer.isEmpty { commitBuffer(client: client) }
                        BanglishInputController.isBanglaMode.toggle()
                    }
                }
            }
            return false // Pass flagsChanged through so system shortcuts and macOS input switching are never blocked
        }

        guard event.type == .keyDown else {
            return false
        }

        // If Fn key was held down while pressing a key, mark it as a combination key
        if Self.fnKeyDownTime > 0 {
            Self.fnUsedWithOtherKey = true
        }

        let modifiers = event.modifierFlags
        let keyCode = event.keyCode
        let char = event.characters?.first ?? event.charactersIgnoringModifiers?.first

        // Dedicated hardware Fn / Globe key (keycode 63) keyDown fallback
        if keyCode == 63 {
            if !buffer.isEmpty { commitBuffer(client: client) }
            BanglishInputController.isBanglaMode.toggle()
            return true
        }

        // Check modifier keys cleanly
        let isCommand = modifiers.contains(.command) ||
                        modifiers.intersection(.deviceIndependentFlagsMask).contains(.command) ||
                        (modifiers.rawValue & NSEvent.ModifierFlags.command.rawValue) != 0
        let isControl = modifiers.contains(.control) ||
                        modifiers.intersection(.deviceIndependentFlagsMask).contains(.control) ||
                        (modifiers.rawValue & NSEvent.ModifierFlags.control.rawValue) != 0
        let isOption = modifiers.contains(.option) ||
                       modifiers.intersection(.deviceIndependentFlagsMask).contains(.option) ||
                       (modifiers.rawValue & NSEvent.ModifierFlags.option.rawValue) != 0

        // Universal Language Toggle Shortcuts:
        // 1) Option + Space (⌥ + Space)
        // 2) Control + Space (^ + Space - default macOS input source shortcut)
        let isToggleShortcut = (isOption || isControl) && !isCommand && keyCode == 49
        if isToggleShortcut {
            if !buffer.isEmpty { commitBuffer(client: client) }
            BanglishInputController.isBanglaMode.toggle()
            return true
        }

        // Pass through ANY command shortcuts (Cmd+C, Cmd+V, Cmd+A, Cmd+Z, Cmd+S, Cmd+X, Cmd+Q, Cmd+W, etc.)
        // Never eat or corrupt user clipboard / editor actions!
        if isCommand {
            if !buffer.isEmpty {
                buffer.removeAll()
                CandidateWindow.shared.hide()
            }
            return false
        }

        // Pass through Control or Option application shortcuts (e.g. Ctrl+Tab, Option+Arrow, etc. where keyCode != 49)
        if isControl || (isOption && keyCode != 49) {
            if !buffer.isEmpty {
                buffer.removeAll()
                CandidateWindow.shared.hide()
            }
            return false
        }

        if !BanglishInputController.isBanglaMode {
            return false
        }

        // Number keys 1...9 (Top Number Row & Numeric Keypad) to select candidate
        let numberKeyCodes: [UInt16: Int] = [
            // Top Number Row (1-9)
            18: 0, // 1
            19: 1, // 2
            20: 2, // 3
            21: 3, // 4
            23: 4, // 5
            22: 5, // 6
            26: 6, // 7
            28: 7, // 8
            25: 8, // 9
            // Full Keyboard Numeric Keypad (Keypad 1-9)
            83: 0, // Keypad 1
            84: 1, // Keypad 2
            85: 2, // Keypad 3
            86: 3, // Keypad 4
            87: 4, // Keypad 5
            88: 5, // Keypad 6
            89: 6, // Keypad 7
            91: 7, // Keypad 8
            92: 8  // Keypad 9
        ]
        if CandidateWindow.shared.isCandidateWindowVisible,
           !buffer.allSatisfy({ $0.isNumber }),
           !modifiers.contains(.command), !modifiers.contains(.control), !modifiers.contains(.option), !modifiers.contains(.shift),
           let idx = numberKeyCodes[keyCode],
           idx < CandidateWindow.shared.candidateCount {
            CandidateWindow.shared.selectIndex(idx)
            commitBuffer(client: client)
            return true
        }

        // Up Arrow (keycode 126): Move candidate selection up
        if keyCode == 126 && CandidateWindow.shared.isCandidateWindowVisible {
            CandidateWindow.shared.selectPrevious()
            if let chosen = CandidateWindow.shared.selectedCandidate() {
                updateMarkedText(client: client, customText: chosen)
            }
            return true
        }

        // Down Arrow (keycode 125): Move candidate selection down
        if keyCode == 125 && CandidateWindow.shared.isCandidateWindowVisible {
            CandidateWindow.shared.selectNext()
            if let chosen = CandidateWindow.shared.selectedCandidate() {
                updateMarkedText(client: client, customText: chosen)
            }
            return true
        }

        // Tab (keycode 48): Cycle candidates if popup is open
        if keyCode == 48 {
            if CandidateWindow.shared.isCandidateWindowVisible {
                CandidateWindow.shared.selectNext()
                if let chosen = CandidateWindow.shared.selectedCandidate() {
                    updateMarkedText(client: client, customText: chosen)
                }
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

        // Navigation, Editing, Function & Numpad Operator Keys (Full Keyboard)
        // Arrows (123, 124), Home (115), End (119), PgUp (116), PgDn (121), Del (117), Ins (114)
        // Numpad Operators: + (69), - (78), * (67), / (75), . (65), Clear (71), = (81)
        // Function Keys: F1-F12 (122, 120, 99, 118, 96, 97, 98, 100, 101, 109, 103, 111)
        let isNavOrSpecial = keyCode == 123 || keyCode == 124 || keyCode == 115 || keyCode == 119 ||
                             keyCode == 116 || keyCode == 121 || keyCode == 117 || keyCode == 114 ||
                             keyCode == 69 || keyCode == 78 || keyCode == 67 || keyCode == 75 || keyCode == 65 || keyCode == 71 || keyCode == 81 ||
                             (keyCode >= 96 && keyCode <= 101) || keyCode == 103 || keyCode == 109 || keyCode == 111 || keyCode == 118 || keyCode == 120 || keyCode == 122
        if isNavOrSpecial {
            if !buffer.isEmpty { commitBuffer(client: client) }
            return false
        }

        // Full stop / Daari (.)
        if char == "." {
            if !buffer.isEmpty {
                if buffer == "." {
                    // Typing dot again ("..") directly commits "."
                    client.insertText("." as NSString, replacementRange: Self.emptyRange)
                    buffer.removeAll()
                    CandidateWindow.shared.hide()
                    return true
                }
                // Commit previous buffer word first
                commitBuffer(client: client)
            }
            buffer = "."
            updateMarkedText(client: client)
            updateCandidates(client: client)
            return true
        }

        // Digits 0...9 direct insertion (Bengali numerals)
        // Directly insert Bengali numerals to avoid composition / marked-text replacement
        // bugs in non-standard editors like Adobe After Effects, Illustrator, Photoshop, etc.
        let banglaDigits: [Character: String] = [
            "0": "০", "1": "১", "2": "২", "3": "৩", "4": "৪",
            "5": "৫", "6": "৬", "7": "৭", "8": "৮", "9": "৯"
        ]
        if let char = char, let bDigit = banglaDigits[char],
           !modifiers.contains(.command), !modifiers.contains(.control), !modifiers.contains(.option), !modifiers.contains(.shift) {
            if !buffer.isEmpty {
                commitBuffer(client: client)
            }
            client.insertText(bDigit as NSString, replacementRange: Self.emptyRange)
            return true
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
    private func updateMarkedText(client: any IMKTextInput, customText: String? = nil) {
        if buffer.isEmpty {
            clearMarkedText(client: client)
            return
        }

        let converted: String
        if let custom = customText {
            converted = custom
        } else if buffer == "." {
            converted = "।"
        } else {
            let candidates = BanglishDictionary.shared.candidates(for: buffer)
            converted = candidates.first ?? engine.transliterate(buffer)
        }

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

        // Pure numbers (e.g. 1, 12, 1234567890) should never show floating candidate window
        if buffer.allSatisfy({ $0.isNumber }) {
            CandidateWindow.shared.hide()
            return
        }

        let candidates = BanglishDictionary.shared.candidates(for: buffer)
        guard !candidates.isEmpty else {
            CandidateWindow.shared.hide()
            return
        }

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
        let fallback = (buffer == ".") ? "।" : engine.transliterate(buffer)
        let textToCommit = CandidateWindow.shared.selectedCandidate() ?? fallback
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
