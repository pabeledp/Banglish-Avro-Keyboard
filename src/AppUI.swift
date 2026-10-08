import Cocoa

// MARK: - Color Palette & Helpers (Inspired by Reference Design)
extension NSColor {
    convenience init(hex: UInt32, alpha: CGFloat = 1.0) {
        let r = CGFloat((hex >> 16) & 0xFF) / 255.0
        let g = CGFloat((hex >> 8) & 0xFF) / 255.0
        let b = CGFloat(hex & 0xFF) / 255.0
        self.init(red: r, green: g, blue: b, alpha: alpha)
    }

    static let wineDark = NSColor(hex: 0x3B131E)        // Deep rich burgundy/wine from reference
    static let wineMedium = NSColor(hex: 0x8A2A45)      // Elegant berry wine accent
    static let wineLight = NSColor(hex: 0xFAF0EB)       // Warm blush pill background
    static let textMuted = NSColor(hex: 0x7A5B64)       // Soft readable subtitle text
    static let cardBorder = NSColor(hex: 0xEDE0D8)      // Subtle warm glass border
    static let canvasBgStart = NSColor(hex: 0xFFFAF7)   // Warm cream glow
    static let canvasBgEnd = NSColor(hex: 0xF7ECE5)     // Soft peach base
}

// MARK: - Custom Liquid Glass Background with Subtle Canvas Grid
final class LiquidGlassCanvasView: NSView {
    override func draw(_ dirtyRect: NSRect) {
        super.draw(dirtyRect)

        // 1. Warm Radiant Background Gradient
        let gradient = NSGradient(colors: [.canvasBgStart, .canvasBgEnd])
        gradient?.draw(in: bounds, angle: -45)

        // 2. Subtle Design Grid Pattern (like reference hero background)
        guard let ctx = NSGraphicsContext.current?.cgContext else { return }
        ctx.saveGState()
        ctx.setStrokeColor(NSColor(hex: 0xEADBD2, alpha: 0.35).cgColor)
        ctx.setLineWidth(1.0)

        let gridSize: CGFloat = 36.0
        var x: CGFloat = 0
        while x < bounds.width {
            ctx.move(to: CGPoint(x: x, y: 0))
            ctx.addLine(to: CGPoint(x: x, y: bounds.height))
            x += gridSize
        }

        var y: CGFloat = 0
        while y < bounds.height {
            ctx.move(to: CGPoint(x: 0, y: y))
            ctx.addLine(to: CGPoint(x: bounds.width, y: y))
            y += gridSize
        }
        ctx.strokePath()
        ctx.restoreGState()
    }
}

// MARK: - Custom Floating Glass Card Box
final class FloatingGlassCard: NSBox {
    init(frame: NSRect, radius: CGFloat = 18) {
        super.init(frame: frame)
        self.boxType = .custom
        self.titlePosition = .noTitle
        self.cornerRadius = radius
        self.borderWidth = 1.0
        self.borderColor = .cardBorder
        self.fillColor = NSColor(white: 1.0, alpha: 0.94)
        
        let shadow = NSShadow()
        shadow.shadowColor = NSColor(hex: 0x3B131E, alpha: 0.08)
        shadow.shadowOffset = NSSize(width: 0, height: -6)
        shadow.shadowBlurRadius = 22
        self.shadow = shadow
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }
}

// MARK: - Pill Button Component
final class PillActionButton: NSButton {
    private let isPrimary: Bool

    init(title: String, isPrimary: Bool = true, target: AnyObject?, action: Selector) {
        self.isPrimary = isPrimary
        super.init(frame: .zero)
        self.title = title
        self.target = target
        self.action = action
        self.bezelStyle = .regularSquare
        self.isBordered = false
        self.wantsLayer = true
        self.layer?.cornerRadius = 18
        self.layer?.masksToBounds = true
        
        if isPrimary {
            self.layer?.backgroundColor = NSColor.wineDark.cgColor
            let attr = NSMutableAttributedString(string: title, attributes: [
                .foregroundColor: NSColor.white,
                .font: NSFont.systemFont(ofSize: 13, weight: .bold)
            ])
            self.attributedTitle = attr
        } else {
            self.layer?.backgroundColor = NSColor.white.cgColor
            self.layer?.borderWidth = 1.2
            self.layer?.borderColor = NSColor.cardBorder.cgColor
            let attr = NSMutableAttributedString(string: title, attributes: [
                .foregroundColor: NSColor.wineDark,
                .font: NSFont.systemFont(ofSize: 13, weight: .semibold)
            ])
            self.attributedTitle = attr
        }
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }
}

// MARK: - App Coordinator & Window Setup
final class BanglishAppCoordinator: NSObject, NSApplicationDelegate {
    private var mainWindow: NSWindow?

    func applicationDidFinishLaunching(_ notification: Notification) {
        // Pure background input method
    }

    @objc func openSystemKeyboardSettings() {
        if let url = URL(string: "x-apple.systempreferences:com.apple.Keyboard-Settings.extension") {
            NSWorkspace.shared.open(url)
        }
    }

    @objc func toggleMode() {
        BanglishInputController.isBanglaMode.toggle()
    }

    // MARK: - Modern Apple Liquid Glass Window UI
    @objc func showMainWindow() {
        if mainWindow == nil {
            let winWidth: CGFloat = 820
            let winHeight: CGFloat = 660
            let rect = NSRect(x: 0, y: 0, width: winWidth, height: winHeight)

            let window = NSWindow(
                contentRect: rect,
                styleMask: [.titled, .closable, .miniaturizable, .fullSizeContentView],
                backing: .buffered,
                defer: false
            )
            window.title = "Banglish"
            window.titlebarAppearsTransparent = true
            window.titleVisibility = .hidden
            window.center()
            window.isReleasedWhenClosed = false

            let canvas = LiquidGlassCanvasView(frame: rect)
            canvas.autoresizingMask = [.width, .height]

            // 1. Top Navbar / Floating Header Pill
            let topPill = NSBox(frame: NSRect(x: 36, y: winHeight - 64, width: winWidth - 72, height: 44))
            topPill.boxType = .custom
            topPill.cornerRadius = 22
            topPill.borderWidth = 1.0
            topPill.borderColor = .cardBorder
            topPill.fillColor = NSColor(white: 1.0, alpha: 0.92)
            
            let logoThumb = NSImageView(frame: NSRect(x: 14, y: 7, width: 30, height: 30))
            let logoPath = "/Users/rmacstudio2/Documents/Banglish/Banglish-Logo.png"
            if let logoImg = NSImage(contentsOfFile: logoPath) {
                logoThumb.image = logoImg
            } else {
                logoThumb.image = NSApp.applicationIconImage
            }
            logoThumb.wantsLayer = true
            logoThumb.layer?.cornerRadius = 8
            logoThumb.layer?.masksToBounds = true
            topPill.addSubview(logoThumb)

            let navTitle = NSTextField(labelWithString: "Banglish")
            navTitle.frame = NSRect(x: 52, y: 12, width: 120, height: 20)
            navTitle.font = NSFont.systemFont(ofSize: 14, weight: .bold)
            navTitle.textColor = .wineDark
            topPill.addSubview(navTitle)

            let statusPill = NSTextField(labelWithString: "● একটিভ: মেনুবারে [ ব ] চালু আছে")
            statusPill.frame = NSRect(x: topPill.frame.width - 240, y: 12, width: 220, height: 20)
            statusPill.alignment = .right
            statusPill.font = NSFont.systemFont(ofSize: 12, weight: .semibold)
            statusPill.textColor = NSColor(hex: 0x188038)
            topPill.addSubview(statusPill)

            canvas.addSubview(topPill)

            // 2. Left Brand Card (Floating Profile Style from Reference)
            let leftCardWidth: CGFloat = 260
            let leftCardHeight: CGFloat = 340
            let leftCard = FloatingGlassCard(frame: NSRect(x: 36, y: winHeight - 425, width: leftCardWidth, height: leftCardHeight), radius: 22)

            let bigLogoView = NSImageView(frame: NSRect(x: 30, y: leftCardHeight - 210, width: 200, height: 180))
            if let logoImg = NSImage(contentsOfFile: logoPath) {
                bigLogoView.image = logoImg
            }
            bigLogoView.wantsLayer = true
            bigLogoView.layer?.cornerRadius = 18
            bigLogoView.layer?.masksToBounds = true
            leftCard.addSubview(bigLogoView)

            let brandLabel = NSTextField(labelWithString: "Banglish 1.0")
            brandLabel.frame = NSRect(x: 20, y: 70, width: 220, height: 24)
            brandLabel.font = NSFont.systemFont(ofSize: 17, weight: .bold)
            brandLabel.textColor = .wineDark
            leftCard.addSubview(brandLabel)

            let roleLabel = NSTextField(labelWithString: "Native macOS Bangla IME")
            roleLabel.frame = NSRect(x: 20, y: 48, width: 220, height: 18)
            roleLabel.font = NSFont.systemFont(ofSize: 12, weight: .medium)
            roleLabel.textColor = .textMuted
            leftCard.addSubview(roleLabel)

            let ratingPill = NSTextField(labelWithString: "★★★★★ 100% Free & Open")
            ratingPill.frame = NSRect(x: 20, y: 18, width: 220, height: 18)
            ratingPill.font = NSFont.systemFont(ofSize: 11, weight: .semibold)
            ratingPill.textColor = NSColor(hex: 0xC86E18)
            leftCard.addSubview(ratingPill)

            canvas.addSubview(leftCard)

            // 3. Right Hero Title & Subtitle Section
            let rightX: CGFloat = 320
            let rightWidth: CGFloat = winWidth - rightX - 36

            // Badge Pill
            let heroBadge = NSBox(frame: NSRect(x: rightX, y: winHeight - 118, width: 260, height: 26))
            heroBadge.boxType = .custom
            heroBadge.cornerRadius = 13
            heroBadge.borderWidth = 1.0
            heroBadge.borderColor = NSColor(hex: 0xE8D5CE)
            heroBadge.fillColor = NSColor.wineLight
            let badgeText = NSTextField(labelWithString: "✨ NATIVE BANGLA PHONETIC ENGINE")
            badgeText.frame = NSRect(x: 10, y: 4, width: 240, height: 16)
            badgeText.font = NSFont.systemFont(ofSize: 10.5, weight: .bold)
            badgeText.textColor = .wineDark
            heroBadge.addSubview(badgeText)
            canvas.addSubview(heroBadge)

            // Main Hero Heading (Wine Dark + Berry Accent)
            let heroTitle = NSTextField(labelWithString: "High-Impact, Smooth &\nNative Bangla Typing")
            heroTitle.frame = NSRect(x: rightX, y: winHeight - 195, width: rightWidth, height: 70)
            heroTitle.font = NSFont.systemFont(ofSize: 26, weight: .heavy)
            heroTitle.textColor = .wineDark
            canvas.addSubview(heroTitle)

            // Subtitle Description
            let heroDesc = NSTextField(labelWithString: "ম্যাকের সব অ্যাপে (Safari, Notes, Word, Adobe) ঝামেলাহীন ও মসৃণ ফনেটিক বাংলা লেখার প্রিমিয়াম অভিজ্ঞতা।")
            heroDesc.frame = NSRect(x: rightX, y: winHeight - 245, width: rightWidth, height: 42)
            heroDesc.font = NSFont.systemFont(ofSize: 12.5, weight: .medium)
            heroDesc.textColor = .textMuted
            canvas.addSubview(heroDesc)

            // 4. Primary Interactive Playground Card (Video Track / Editor Style from Reference)
            let cardY: CGFloat = 175
            let playCard = FloatingGlassCard(frame: NSRect(x: rightX, y: cardY, width: rightWidth, height: 215), radius: 20)

            let playTitle = NSTextField(labelWithString: "✍️ লাইভ টাইপিং প্লেগ্রাউন্ড:")
            playTitle.frame = NSRect(x: 18, y: 178, width: 250, height: 20)
            playTitle.font = NSFont.systemFont(ofSize: 13, weight: .bold)
            playTitle.textColor = .wineDark
            playCard.addSubview(playTitle)

            // Input Field (Rounded with peach tint)
            let inputField = NSTextField(frame: NSRect(x: 18, y: 124, width: rightWidth - 36, height: 44))
            inputField.font = NSFont.systemFont(ofSize: 15)
            inputField.placeholderString = "ইংরেজি অক্ষরে লিখুন: 'ami bangla bhalobasi'..."
            inputField.focusRingType = .none
            inputField.wantsLayer = true
            inputField.layer?.cornerRadius = 10
            inputField.layer?.borderWidth = 1.0
            inputField.layer?.borderColor = NSColor(hex: 0xE8D8D0).cgColor
            inputField.layer?.backgroundColor = NSColor(hex: 0xFAF6F3).cgColor
            playCard.addSubview(inputField)

            // Live Output Bangla Result
            let outputField = NSTextField(labelWithString: "আমি বাংলা ভালোবাসি")
            outputField.frame = NSRect(x: 18, y: 56, width: rightWidth - 160, height: 45)
            outputField.font = NSFont.systemFont(ofSize: 24, weight: .bold)
            outputField.textColor = .wineMedium
            playCard.addSubview(outputField)

            // Copy Action Button
            let copyBtn = PillActionButton(title: "কপি করুন", isPrimary: true, target: self, action: #selector(copyBanglaText))
            copyBtn.frame = NSRect(x: rightWidth - 125, y: 62, width: 105, height: 36)
            playCard.addSubview(copyBtn)

            // Live translation observer
            NotificationCenter.default.addObserver(forName: NSControl.textDidChangeNotification, object: inputField, queue: .main) { _ in
                let txt = inputField.stringValue
                if txt.isEmpty {
                    outputField.stringValue = "আমি বাংলা ভালোবাসি"
                    outputField.textColor = NSColor(hex: 0xBFA8A0)
                } else {
                    let words = txt.components(separatedBy: " ")
                    let res = words.map { BanglishEngine.shared.transliterate($0) }.joined(separator: " ")
                    outputField.stringValue = res
                    outputField.textColor = .wineMedium
                }
            }

            // Timeline Track / Status indicator at bottom of card
            let trackStrip = NSBox(frame: NSRect(x: 18, y: 16, width: rightWidth - 36, height: 26))
            trackStrip.boxType = .custom
            trackStrip.cornerRadius = 6
            trackStrip.borderWidth = 0
            trackStrip.fillColor = NSColor(hex: 0xFAF2EB)
            let trackLabel = NSTextField(labelWithString: "⚡ লাইভ ইঞ্জিন: Avro ফনেটিক রুলস অনুযায়ী স্বয়ংক্রিয় অক্ষর প্রসেস হচ্ছে")
            trackLabel.frame = NSRect(x: 8, y: 4, width: rightWidth - 52, height: 16)
            trackLabel.font = NSFont.systemFont(ofSize: 10.5, weight: .semibold)
            trackLabel.textColor = .textMuted
            trackStrip.addSubview(trackLabel)
            playCard.addSubview(trackStrip)

            canvas.addSubview(playCard)

            // 5. Quick Rules & Shortcuts Strip
            let rulesBox = FloatingGlassCard(frame: NSRect(x: 36, y: 76, width: winWidth - 72, height: 82), radius: 16)
            
            let rulesHeader = NSTextField(labelWithString: "💡 সহজে লেখার নিয়ম:")
            rulesHeader.frame = NSRect(x: 16, y: 52, width: 180, height: 18)
            rulesHeader.font = NSFont.systemFont(ofSize: 11.5, weight: .bold)
            rulesHeader.textColor = .wineDark
            rulesBox.addSubview(rulesHeader)

            let rulesContent = NSTextField(labelWithString: "• ami ➔ আমি    • bangla ➔ বাংলা    • sundor ➔ সুন্দর    • kormo ➔ কর্ম    • Dhaka ➔ ঢাকা    • t`` ➔ ৎ    • . ➔ ।    • $ ➔ ৳\n• কীবোর্ড শর্টকাট: যেকোনো অ্যাপে Option + Space (⌥ + Space) চাপলে সরাসরি বাংলা ও ইংরেজির মধ্যে সুইচ হবে।")
            rulesContent.frame = NSRect(x: 16, y: 8, width: winWidth - 110, height: 42)
            rulesContent.font = NSFont.systemFont(ofSize: 11.5)
            rulesContent.textColor = .textMuted
            rulesBox.addSubview(rulesContent)

            canvas.addSubview(rulesBox)

            // 6. Bottom Action Bar (Pill Buttons matching reference)
            let openSettingsBtn = PillActionButton(
                title: "কীবোর্ড সেটিংস খুলুন ➔",
                isPrimary: true,
                target: self,
                action: #selector(openSystemKeyboardSettings)
            )
            openSettingsBtn.frame = NSRect(x: 36, y: 20, width: 220, height: 40)
            canvas.addSubview(openSettingsBtn)

            let testGuideBtn = PillActionButton(
                title: "মেনুবার [ ব ] থেকে সিলেক্ট করুন ↗",
                isPrimary: false,
                target: self,
                action: #selector(toggleMode)
            )
            testGuideBtn.frame = NSRect(x: 270, y: 20, width: 260, height: 40)
            canvas.addSubview(testGuideBtn)

            let copyright = NSTextField(labelWithString: "Banglish • Made with love for Bangla on macOS")
            copyright.frame = NSRect(x: winWidth - 320, y: 28, width: 284, height: 20)
            copyright.alignment = .right
            copyright.font = NSFont.systemFont(ofSize: 11)
            copyright.textColor = NSColor(hex: 0xB59E98)
            canvas.addSubview(copyright)

            window.contentView = canvas
            self.mainWindow = window
        }

        mainWindow?.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }

    @objc func copyBanglaText() {
        guard let window = mainWindow,
              let view = window.contentView else { return }
        
        for sub in view.subviews {
            if let box = sub as? FloatingGlassCard {
                for inner in box.subviews {
                    if let label = inner as? NSTextField, label.font?.pointSize == 24 {
                        let pb = NSPasteboard.general
                        pb.clearContents()
                        pb.setString(label.stringValue, forType: .string)
                        
                        let alert = NSAlert()
                        alert.messageText = "কপি করা হয়েছে!"
                        alert.informativeText = "'\(label.stringValue)' সফলভাবে ক্লিপবোর্ডে কপি করা হয়েছে।"
                        alert.alertStyle = .informational
                        alert.runModal()
                        return
                    }
                }
            }
        }
    }

    @objc func quitApp() {
        NSApplication.shared.terminate(nil)
    }
}
