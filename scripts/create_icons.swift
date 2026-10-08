import Cocoa

func generateIcon(size: CGFloat, filename: String) {
    let image = NSImage(size: NSSize(width: size, height: size))
    image.lockFocus()

    // Draw rounded background
    let rect = NSRect(x: 1, y: 1, width: size - 2, height: size - 2)
    let path = NSBezierPath(roundedRect: rect, xRadius: size * 0.22, yRadius: size * 0.22)
    NSColor(calibratedRed: 0.12, green: 0.53, blue: 0.38, alpha: 1.0).setFill() // Deep green
    path.fill()

    // Inner subtle border
    NSColor(calibratedWhite: 1.0, alpha: 0.25).setStroke()
    path.lineWidth = size * 0.04
    path.stroke()

    // Draw Bangla 'ব' letter
    let text = "ব"
    let fontSize = size * 0.62
    let font = NSFont.boldSystemFont(ofSize: fontSize)
    let style = NSMutableParagraphStyle()
    style.alignment = .center

    let attributes: [NSAttributedString.Key: Any] = [
        .font: font,
        .foregroundColor: NSColor.white,
        .paragraphStyle: style
    ]

    let attrStr = NSAttributedString(string: text, attributes: attributes)
    let textSize = attrStr.size()
    let textRect = NSRect(
        x: (size - textSize.width) / 2.0,
        y: (size - textSize.height) / 2.0 + (size * 0.03),
        width: textSize.width,
        height: textSize.height
    )

    attrStr.draw(in: textRect)

    image.unlockFocus()

    if let tiffData = image.tiffRepresentation,
       let rep = NSBitmapImageRep(data: tiffData),
       let pngData = rep.representation(using: .png, properties: [:]) {
        let url = URL(fileURLWithPath: filename)
        try? pngData.write(to: url)
    }

    if let tiffData = image.tiffRepresentation {
        let tiffUrl = URL(fileURLWithPath: filename.replacingOccurrences(of: ".png", with: ".tiff"))
        try? tiffData.write(to: tiffUrl)
    }
}

// Generate menu icons
generateIcon(size: 16, filename: "Resources/icon16.png")
generateIcon(size: 32, filename: "Resources/icon32.png")
generateIcon(size: 128, filename: "Resources/AppIcon.png")
print("Icons generated successfully!")
