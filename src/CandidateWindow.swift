import Cocoa

/// Custom Floating Candidate Window matching Banglish Theme & Avro Reference
public final class CandidateWindow: NSPanel {
    public static let shared = CandidateWindow()

    private var candidates: [String] = []
    private var selectedIndex: Int = 0
    private var onCandidateSelected: ((String) -> Void)?

    private let containerView = NSVisualEffectView()
    private var rowViews: [CandidateRowView] = []
    private let stackView = NSStackView()

    private init() {
        super.init(
            contentRect: NSRect(x: 0, y: 0, width: 160, height: 180),
            styleMask: [.borderless, .nonactivatingPanel],
            backing: .buffered,
            defer: false
        )

        self.isOpaque = false
        self.backgroundColor = .clear
        self.level = .popUpMenu
        self.hasShadow = true
        self.ignoresMouseEvents = false
        self.isReleasedWhenClosed = false
        self.hidesOnDeactivate = false

        setupUI()
    }

    private func setupUI() {
        // Background container with dark green glass theme
        containerView.material = .hudWindow
        containerView.state = .active
        containerView.blendingMode = .behindWindow
        containerView.wantsLayer = true
        containerView.layer?.cornerRadius = 10.0
        containerView.layer?.masksToBounds = true
        containerView.layer?.borderWidth = 1.0
        containerView.layer?.borderColor = NSColor(hex: 0x2D6A4F, alpha: 0.6).cgColor
        containerView.layer?.backgroundColor = NSColor(hex: 0x0B2217, alpha: 0.96).cgColor

        contentView = containerView

        // Stack view for vertical candidates
        stackView.orientation = .vertical
        stackView.alignment = .leading
        stackView.spacing = 2
        stackView.edgeInsets = NSEdgeInsets(top: 4, left: 4, bottom: 4, right: 4)
        stackView.translatesAutoresizingMaskIntoConstraints = false

        containerView.addSubview(stackView)

        NSLayoutConstraint.activate([
            stackView.topAnchor.constraint(equalTo: containerView.topAnchor, constant: 4),
            stackView.bottomAnchor.constraint(equalTo: containerView.bottomAnchor, constant: -4),
            stackView.leadingAnchor.constraint(equalTo: containerView.leadingAnchor, constant: 4),
            stackView.trailingAnchor.constraint(equalTo: containerView.trailingAnchor, constant: -4)
        ])
    }

    /// Present candidates near the active cursor rectangle
    public func show(candidates: [String], near cursorRect: NSRect, onSelect: @escaping (String) -> Void) {
        guard !candidates.isEmpty else {
            hide()
            return
        }

        self.candidates = candidates
        self.selectedIndex = 0
        self.onCandidateSelected = onSelect

        updateRows()
        layoutAndPosition(near: cursorRect)
        orderFront(nil)
    }

    public func hide() {
        orderOut(nil)
        candidates.removeAll()
        selectedIndex = 0
        onCandidateSelected = nil
    }

    public var isCandidateWindowVisible: Bool {
        return isVisible && !candidates.isEmpty
    }

    public func selectedCandidate() -> String? {
        guard selectedIndex >= 0 && selectedIndex < candidates.count else {
            return candidates.first
        }
        return candidates[selectedIndex]
    }

    public func selectNext() {
        guard !candidates.isEmpty else { return }
        selectedIndex = (selectedIndex + 1) % candidates.count
        refreshSelection()
    }

    public func selectPrevious() {
        guard !candidates.isEmpty else { return }
        selectedIndex = (selectedIndex - 1 + candidates.count) % candidates.count
        refreshSelection()
    }

    public func selectIndex(_ index: Int) {
        guard index >= 0 && index < candidates.count else { return }
        selectedIndex = index
        refreshSelection()
        commitCurrentSelection()
    }

    public func commitCurrentSelection() {
        guard let candidate = selectedCandidate() else { return }
        let callback = onCandidateSelected
        hide()
        callback?(candidate)
    }

    private func refreshSelection() {
        for (i, row) in rowViews.enumerated() {
            row.setSelected(i == selectedIndex)
        }
    }

    private func updateRows() {
        // Clear old rows
        for view in stackView.arrangedSubviews {
            stackView.removeArrangedSubview(view)
            view.removeFromSuperview()
        }
        rowViews.removeAll()

        for (i, candidate) in candidates.enumerated() {
            let isLastRaw = (i == candidates.count - 1 && candidate.range(of: "^[a-zA-Z0-9]+$", options: .regularExpression) != nil)
            let row = CandidateRowView(text: candidate, index: i, isSelected: (i == selectedIndex), isRawLatin: isLastRaw) { [weak self] clickedIndex in
                self?.selectIndex(clickedIndex)
            }
            rowViews.append(row)
            stackView.addArrangedSubview(row)
        }
    }

    private func layoutAndPosition(near cursorRect: NSRect) {
        // Calculate dynamic width based on candidates
        var maxTextWidth: CGFloat = 110
        let font = NSFont.systemFont(ofSize: 15, weight: .regular)
        let attrs: [NSAttributedString.Key: Any] = [.font: font]

        for candidate in candidates {
            let strWidth = (candidate as NSString).size(withAttributes: attrs).width
            if strWidth > maxTextWidth {
                maxTextWidth = strWidth
            }
        }

        let panelWidth = max(130, maxTextWidth + 48)
        let rowHeight: CGFloat = 30
        let panelHeight = CGFloat(candidates.count) * (rowHeight + 2) + 8

        for row in rowViews {
            row.widthAnchor.constraint(equalToConstant: panelWidth - 8).isActive = true
            row.heightAnchor.constraint(equalToConstant: rowHeight).isActive = true
        }

        // Determine reference point and height
        var refPoint: NSPoint
        var refHeight: CGFloat = 22.0
        let hasValidCursor = cursorRect.origin.x > 10 && cursorRect.origin.y > 10 && (cursorRect.width > 0 || cursorRect.height > 0)

        if hasValidCursor {
            refPoint = cursorRect.origin
            refHeight = max(20.0, cursorRect.height)
        } else {
            // Fallback: Locate active window or mouse
            var detectedPoint: NSPoint? = nil
            if let frontApp = NSWorkspace.shared.frontmostApplication,
               let windowList = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] {
                for win in windowList {
                    let pid = win[kCGWindowOwnerPID as String] as? pid_t
                    let layer = win[kCGWindowLayer as String] as? Int ?? -1
                    if pid == frontApp.processIdentifier && layer == 0 {
                        if let boundsDict = win[kCGWindowBounds as String] as? [String: Any],
                           let winX = boundsDict["X"] as? CGFloat,
                           let winY = boundsDict["Y"] as? CGFloat,
                           let winW = boundsDict["Width"] as? CGFloat,
                           let winH = boundsDict["Height"] as? CGFloat {
                            let screenH = NSScreen.main?.frame.height ?? 1080
                            let cocoaWinY = screenH - (winY + winH)
                            let winRect = NSRect(x: winX, y: cocoaWinY, width: winW, height: winH)

                            let mouseLoc = NSEvent.mouseLocation
                            if NSPointInRect(mouseLoc, winRect) {
                                detectedPoint = mouseLoc
                            } else {
                                detectedPoint = NSPoint(x: winRect.midX - (panelWidth / 2), y: winRect.minY + 60)
                            }
                            break
                        }
                    }
                }
            }
            refPoint = detectedPoint ?? NSEvent.mouseLocation
        }

        let targetScreen = NSScreen.screens.first { NSPointInRect(refPoint, $0.frame) } ?? NSScreen.main ?? NSScreen.screens[0]
        let visibleFrame = targetScreen.visibleFrame

        var originX = refPoint.x
        // Default: directly below the text / input box
        var originY = refPoint.y - panelHeight - 6

        // If below screen, position directly ABOVE the text / input box
        if originY < visibleFrame.minY {
            originY = refPoint.y + refHeight + 6
        }

        // Clamp horizontally within screen
        if originX + panelWidth > visibleFrame.maxX {
            originX = visibleFrame.maxX - panelWidth - 8
        }
        if originX < visibleFrame.minX {
            originX = visibleFrame.minX + 8
        }

        // Final vertical safety bounds
        if originY + panelHeight > visibleFrame.maxY {
            originY = visibleFrame.maxY - panelHeight - 8
        }
        if originY < visibleFrame.minY {
            originY = visibleFrame.minY + 8
        }

        setContentSize(NSSize(width: panelWidth, height: panelHeight))
        setFrameOrigin(NSPoint(x: originX, y: originY))
    }
}

// MARK: - Individual Candidate Row View
private final class CandidateRowView: NSView {
    private let text: String
    private let index: Int
    private var isSelected: Bool
    private let isRawLatin: Bool
    private let onClick: (Int) -> Void

    private let titleLabel = NSTextField(labelWithString: "")

    init(text: String, index: Int, isSelected: Bool, isRawLatin: Bool, onClick: @escaping (Int) -> Void) {
        self.text = text
        self.index = index
        self.isSelected = isSelected
        self.isRawLatin = isRawLatin
        self.onClick = onClick
        super.init(frame: .zero)

        setupRow()
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    private func setupRow() {
        wantsLayer = true
        layer?.cornerRadius = 6.0
        layer?.masksToBounds = true

        titleLabel.translatesAutoresizingMaskIntoConstraints = false
        titleLabel.isBezeled = false
        titleLabel.drawsBackground = false
        titleLabel.isEditable = false
        titleLabel.isSelectable = false
        titleLabel.font = NSFont.systemFont(ofSize: 15, weight: .regular)
        titleLabel.stringValue = text

        addSubview(titleLabel)

        NSLayoutConstraint.activate([
            titleLabel.leadingAnchor.constraint(equalTo: leadingAnchor, constant: 12),
            titleLabel.trailingAnchor.constraint(equalTo: trailingAnchor, constant: -8),
            titleLabel.centerYAnchor.constraint(equalTo: centerYAnchor)
        ])

        updateAppearance()
    }

    func setSelected(_ selected: Bool) {
        self.isSelected = selected
        updateAppearance()
    }

    private func updateAppearance() {
        if isSelected {
            // Selected row: Rich emerald dark green highlight with crisp white text
            layer?.backgroundColor = NSColor(hex: 0x15803D).cgColor
            titleLabel.textColor = NSColor.white
            titleLabel.font = NSFont.systemFont(ofSize: 15, weight: .bold)
        } else {
            layer?.backgroundColor = NSColor.clear.cgColor
            if isRawLatin {
                // Raw typed Latin at the bottom in luminous mint/light green
                titleLabel.textColor = NSColor(hex: 0x86EFAC)
                titleLabel.font = NSFont.systemFont(ofSize: 14, weight: .medium)
            } else {
                titleLabel.textColor = NSColor.white
                titleLabel.font = NSFont.systemFont(ofSize: 15, weight: .regular)
            }
        }
    }

    override func mouseDown(with event: NSEvent) {
        onClick(index)
    }

    override func mouseEntered(with event: NSEvent) {
        setSelected(true)
    }

    override func mouseExited(with event: NSEvent) {
        if !isSelected {
            setSelected(false)
        }
    }
}
