import Foundation

/// Fast Bengali Dictionary & Phonetic Candidate Suggester
public final class BanglishDictionary {
    public static let shared = BanglishDictionary()

    private var words: Set<String> = []
    private var isLoaded = false
    private let queue = DispatchQueue(label: "org.banglish.dictionary.loader", qos: .userInitiated)

    // Common seed words & juktoborno combinations for instant fallback
    private let seedWords: Set<String> = [
        "আমি", "তুমি", "আমরা", "তোমরা", "তারা", "সে", "আপনি", "আপনারা",
        "বাংলাদেশ", "বাংলা", "বাঙলা", "বাংলিশ", "ভাষায়", "ভাষা", "ভাষার",
        "বাজিমাত", "বাজিমাৎ", "বাজীমাত", "বাজীমাৎ", "বাজিকর",
        "শান্তি", "শান্তী", "সান্তি", "আনন্দ", "আনন্দময়", "বন্ধু", "বন্ধুরা",
        "যুক্তবর্ণ", "যুক্ত", "বর্ণ", "জ্ঞান", "বিজ্ঞান", "বিজ্ঞানী", "স্বাধীনতা",
        "ইতিহাস", "পরীক্ষা", "বৃষ্টি", "সন্ধ্যা", "কঠিন", "কঠীন", "খুব", "খূব",
        "পানি", "পানী", "নদী", "নদি", "সূর্য", "সুর্য", "কারণ", "কারন",
        "মানুষ", "মানুস", "দেশ", "দেশের", "বিপদ", "বিপদ্", "হঠাৎ", "হঠাত",
        "সৃষ্টি", "সৃষ্টী", "অনুষ্ঠান", "পুষ্প", "অঙ্ক", "অংক", "সঙ্গ", "সংগ",
        "ভালো", "ভাল", "কেমন", "আছো", "আছেন", "ধন্যবাদ", "স্বাগতম"
    ]

    private init() {
        self.words = seedWords
        loadDictionary()
    }

    /// Load full dictionary from app bundle or resources
    public func loadDictionary() {
        queue.async { [weak self] in
            guard let self = self, !self.isLoaded else { return }

            var wordSet = self.seedWords

            // Check bundle path
            let possibleUrls = [
                Bundle.main.url(forResource: "words", withExtension: "txt"),
                Bundle.main.resourceURL?.appendingPathComponent("words.txt"),
                URL(fileURLWithPath: "/Library/Input Methods/Banglish.app/Contents/Resources/words.txt"),
                URL(fileURLWithPath: "\(NSHomeDirectory())/Library/Input Methods/Banglish.app/Contents/Resources/words.txt")
            ].compactMap { $0 }

            for url in possibleUrls {
                if FileManager.default.fileExists(atPath: url.path) {
                    if let content = try? String(contentsOf: url, encoding: .utf8) {
                        content.enumerateLines { line, _ in
                            let trimmed = line.trimmingCharacters(in: .whitespacesAndNewlines)
                            if !trimmed.isEmpty {
                                wordSet.insert(trimmed)
                            }
                        }
                        break
                    }
                }
            }

            DispatchQueue.main.async {
                self.words = wordSet
                self.isLoaded = true
            }
        }
    }

    /// Check if word is valid in dictionary
    public func contains(_ word: String) -> Bool {
        return words.contains(word)
    }

    /// Generate ranked candidates for raw typed phonetic input
    public func candidates(for rawInput: String) -> [String] {
        guard !rawInput.isEmpty else { return [] }

        let primary = BanglishEngine.shared.transliterate(rawInput)
        guard !primary.isEmpty else { return [rawInput] }

        var result: [String] = []
        var seen: Set<String> = []

        func addCandidate(_ word: String) {
            let trimmed = word.trimmingCharacters(in: .whitespacesAndNewlines)
            if !trimmed.isEmpty && !seen.contains(trimmed) {
                seen.insert(trimmed)
                result.append(trimmed)
            }
        }

        // 1. Direct engine transliteration is always candidate #1
        addCandidate(primary)

        // 2. Generate phonetic variants based on Bengali orthographic ambiguities
        var variants: Set<String> = []

        // Rule A: Final ত <-> ৎ
        if primary.hasSuffix("ত") {
            let base = String(primary.dropLast())
            variants.insert(base + "ৎ")
        } else if primary.hasSuffix("ৎ") {
            let base = String(primary.dropLast())
            variants.insert(base + "ত")
        }

        // Rule B: ি <-> ী (Hroshwo-I vs Deergho-I)
        let curWithI = Array(variants) + [primary]
        for w in curWithI {
            if w.contains("ি") {
                variants.insert(w.replacingOccurrences(of: "ি", with: "ী"))
            }
            if w.contains("ী") {
                variants.insert(w.replacingOccurrences(of: "ী", with: "ি"))
            }
        }

        // Rule C: ু <-> ূ (Hroshwo-U vs Deergho-U)
        let curWithU = Array(variants) + [primary]
        for w in curWithU {
            if w.contains("ু") {
                variants.insert(w.replacingOccurrences(of: "ু", with: "ূ"))
            }
            if w.contains("ূ") {
                variants.insert(w.replacingOccurrences(of: "ূ", with: "ু"))
            }
        }

        // Rule D: স <-> শ <-> ষ (S / Sh / Shh)
        let curWithS = Array(variants) + [primary]
        for w in curWithS {
            if w.contains("স") {
                variants.insert(w.replacingOccurrences(of: "স", with: "শ"))
                variants.insert(w.replacingOccurrences(of: "স", with: "ষ"))
            }
            if w.contains("শ") {
                variants.insert(w.replacingOccurrences(of: "শ", with: "স"))
                variants.insert(w.replacingOccurrences(of: "শ", with: "ষ"))
            }
            if w.contains("ষ") {
                variants.insert(w.replacingOccurrences(of: "ষ", with: "শ"))
                variants.insert(w.replacingOccurrences(of: "ষ", with: "স"))
            }
        }

        // Rule E: ন <-> ণ (Donto-Na vs Murdhonyo-Na)
        let curWithN = Array(variants) + [primary]
        for w in curWithN {
            if w.contains("ন") {
                variants.insert(w.replacingOccurrences(of: "ন", with: "ণ"))
            }
            if w.contains("ণ") {
                variants.insert(w.replacingOccurrences(of: "ণ", with: "ন"))
            }
        }

        // Rule F: র <-> ড় <-> ঢ়
        let curWithR = Array(variants) + [primary]
        for w in curWithR {
            if w.contains("র") {
                variants.insert(w.replacingOccurrences(of: "র", with: "ড়"))
            }
            if w.contains("ড়") {
                variants.insert(w.replacingOccurrences(of: "ড়", with: "র"))
                variants.insert(w.replacingOccurrences(of: "ড়", with: "ঢ়"))
            }
            if w.contains("ঢ়") {
                variants.insert(w.replacingOccurrences(of: "ঢ়", with: "ড়"))
            }
        }

        // Rule G: জ <-> য (Ja vs Ya)
        let curWithJ = Array(variants) + [primary]
        for w in curWithJ {
            if w.contains("জ") {
                variants.insert(w.replacingOccurrences(of: "জ", with: "য"))
            }
            if w.contains("য") {
                variants.insert(w.replacingOccurrences(of: "য", with: "জ"))
            }
        }

        // Rule H: ং <-> ঙ
        let curWithNg = Array(variants) + [primary]
        for w in curWithNg {
            if w.contains("ং") {
                variants.insert(w.replacingOccurrences(of: "ং", with: "ঙ"))
            }
            if w.contains("ঙ") {
                variants.insert(w.replacingOccurrences(of: "ঙ", with: "ং"))
            }
        }

        // Rule I: Common Juktoborno variants
        let curWithJukto = Array(variants) + [primary]
        for w in curWithJukto {
            if w.contains("ন্ত") { variants.insert(w.replacingOccurrences(of: "ন্ত", with: "ণ্ট")) }
            if w.contains("ণ্ট") { variants.insert(w.replacingOccurrences(of: "ণ্ট", with: "ন্ত")) }
            if w.contains("ন্দ") { variants.insert(w.replacingOccurrences(of: "ন্দ", with: "ণ্ড")) }
            if w.contains("ণ্ড") { variants.insert(w.replacingOccurrences(of: "ণ্ড", with: "ন্দ")) }
            if w.contains("স্ত") { variants.insert(w.replacingOccurrences(of: "স্ত", with: "ষ্ট")) }
            if w.contains("ষ্ট") { variants.insert(w.replacingOccurrences(of: "ষ্ট", with: "স্ত")) }
            if w.contains("স্থ") { variants.insert(w.replacingOccurrences(of: "স্থ", with: "ষ্ঠ")) }
            if w.contains("ষ্ঠ") { variants.insert(w.replacingOccurrences(of: "ষ্ঠ", with: "স্থ")) }
            if w.contains("স্প") { variants.insert(w.replacingOccurrences(of: "স্প", with: "ষ্প")) }
            if w.contains("ষ্প") { variants.insert(w.replacingOccurrences(of: "ষ্প", with: "স্প")) }
            if w.contains("ঙ্ক") { variants.insert(w.replacingOccurrences(of: "ঙ্ক", with: "অঙ্ক")) }
            if w.contains("জ্ঞ") { variants.insert(w.replacingOccurrences(of: "জ্ঞ", with: "গ্য")) }
            if w.contains("ক্ষ") { variants.insert(w.replacingOccurrences(of: "ক্ষ", with: "খ")) }
        }

        // Prioritize dictionary confirmed variants
        var dictConfirmed: [String] = []
        var others: [String] = []

        for v in variants {
            if words.contains(v) {
                dictConfirmed.append(v)
            } else {
                others.append(v)
            }
        }

        // Add dictionary confirmed variants first
        for v in dictConfirmed {
            if result.count >= 6 { break }
            addCandidate(v)
        }

        // If list still has room, add other phonetic variants
        for v in others {
            if result.count >= 5 { break }
            addCandidate(v)
        }

        // 3. Always append raw Latin text as the last candidate (exactly like reference)
        addCandidate(rawInput)

        return result
    }
}
