import Foundation

/// Arabizi (English to Arabic) Phonetic Transliteration Engine in Swift
public final class ArabicEngine {
    public static let shared = ArabicEngine()

    public init() {}

    private let vowels: Set<String> = ["a", "u", "i", "o", "e", "A"]

    /// Strip Harakat (Diacritics) for unvocalized search & fallback
    public func stripDiacritics(_ text: String) -> String {
        let diacriticScalars: Set<UInt32> = [
            0x064B, // Fathatan
            0x064C, // Dammatan
            0x064D, // Kasratan
            0x064E, // Fatha
            0x064F, // Damma
            0x0650, // Kasra
            0x0651, // Shaddah
            0x0652  // Sukun
        ]
        let stripped = text.unicodeScalars.filter { !diacriticScalars.contains($0.value) }
        return String(String.UnicodeScalarView(stripped))
    }

    /// Transliterate Arabizi phonetic string into Arabic Unicode
    public func transliterate(_ rawInput: String) -> String {
        guard !rawInput.isEmpty else { return "" }

        let chars = rawInput.map { String($0) }
        let total = chars.count
        var output = ""
        var cur = 0

        while cur < total {
            let isWordStart = (cur == 0) || (cur > 0 && (chars[cur - 1] == " " || chars[cur - 1] == "-"))

            // 1. Initial Vowel Handling: In Arabic, words cannot start with a floating diacritic
            if isWordStart {
                // Initial 'al' -> ال (Alif-Lam)
                if cur + 1 < total && chars[cur].lowercased() == "a" && chars[cur + 1].lowercased() == "l" {
                    output.append("ال")
                    cur += 2
                    continue
                }
                // Initial 'a' -> أ / ا
                if chars[cur] == "a" {
                    output.append("أ")
                    cur += 1
                    continue
                }
                if chars[cur] == "A" || (cur + 1 < total && chars[cur].lowercased() == "a" && chars[cur + 1].lowercased() == "a") {
                    output.append("ا")
                    cur += (chars[cur] == "A") ? 1 : 2
                    continue
                }
                // Initial 'i' -> إ
                if chars[cur].lowercased() == "i" || chars[cur].lowercased() == "e" {
                    output.append("إ")
                    cur += 1
                    continue
                }
                // Initial 'u' / 'o' -> أُ
                if chars[cur].lowercased() == "u" || chars[cur].lowercased() == "o" {
                    output.append("أُ")
                    cur += 1
                    continue
                }
            }

            // 2. Greedy Pattern Matching from ArabicData
            var matched = false
            for pattern in ArabicData.patterns {
                let pFind = pattern.find
                let pLen = pFind.count
                let end = cur + pLen

                if end <= total {
                    let sub = chars[cur..<end].joined()
                    if sub == pFind {
                        // Lam-Alif Ligature check
                        if output.hasSuffix("ل") && (pattern.replace == "ا" || pattern.replace == "أ") {
                            output.removeLast()
                            output.append(ArabicData.lamAlif)
                        } else {
                            output.append(pattern.replace)
                        }
                        cur = end
                        matched = true
                        break
                    }
                }
            }

            if !matched {
                output.append(chars[cur])
                cur += 1
            }
        }

        return output
    }
}
