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
        // Background container with dark glass theme
        containerView.material = .hudWindow
        containerView.state = .active
        containerView.blendingMode = .behindWindow
        containerView.wantsLayer = true
        containerView.layer?.cornerRadius = 10.0
        containerView.layer?.masksToBounds = true
        containerView.layer?.borderWidth = 1.0
        containerView.layer?.borderColor = NSColor(white: 1.0, alpha: 0.16).cgColor
        containerView.layer?.backgroundColor = NSColor(hex: 0x1E1E22, alpha: 0.94).cgColor

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

        // Determine window origin
        var originX = cursorRect.origin.x
        var originY = cursorRect.origin.y - panelHeight - 6

        // Screen boundary safety checks
        let targetScreen = NSScreen.screens.first { NSPointInRect(cursorRect.origin, $0.frame) } ?? NSScreen.main ?? NSScreen.screens[0]
        let visibleFrame = targetScreen.visibleFrame

        // If cursor rect is 0 (app didn't provide coordinates), position near mouse
        if cursorRect.origin.x == 0 && cursorRect.origin.y == 0 {
            let mouseLoc = NSEvent.mouseLocation
            originX = mouseLoc.x + 10
            originY = mouseLoc.y - panelHeight - 10
        }

        if originY < visibleFrame.minY {
            // Position above cursor if near bottom
            originY = cursorRect.origin.y + cursorRect.height + 6
        }
        if originX + panelWidth > visibleFrame.maxX {
            originX = visibleFrame.maxX - panelWidth - 8
        }
        if originX < visibleFrame.minX {
            originX = visibleFrame.minX + 8
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
            // Warm vibrant orange highlight matching Avro reference screenshot
            layer?.backgroundColor = NSColor(hex: 0xF59E0B).cgColor
            titleLabel.textColor = NSColor.black
            titleLabel.font = NSFont.systemFont(ofSize: 15, weight: .medium)
        } else {
            layer?.backgroundColor = NSColor.clear.cgColor
            if isRawLatin {
                // Raw typed Latin at the bottom in warm amber color
                titleLabel.textColor = NSColor(hex: 0xF59E0B)
                titleLabel.font = NSFont.systemFont(ofSize: 14, weight: .regular)
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
