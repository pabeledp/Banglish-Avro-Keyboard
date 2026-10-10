import Foundation

/// Fast Bengali Dictionary & Phonetic Candidate Suggester
public final class BanglishDictionary {
    public static let shared = BanglishDictionary()

    private var words: Set<String> = []
    private var autodict: [String: String] = [:]
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
        "ব্যাংক", "ব্যাঙ্ক", "ইউআই", "ট্যাংক", "সফটওয়্যার", "ভালো", "ভাল", "কেমন", "আছো", "আছেন", "ধন্যবাদ", "স্বাগতম"
    ]

    // Fast built-in autocorrect seed mappings
    private let seedAutodict: [String: String] = [
        "bank": "ব্যাংক",
        "banking": "ব্যাংকিং",
        "iuai": "ইউআই",
        "ui": "ইউআই",
        "ux": "ইউএক্স",
        "ai": "এআই",
        "tank": "ট্যাংক",
        "rank": "র‍্যাংক",
        "ranking": "র‍্যাংকিং",
        "link": "লিংক",
        "pink": "পিংক",
        "sink": "সিংক",
        "thanks": "থ্যাংকস",
        "software": "সফটওয়্যার",
        "hardware": "হার্ডওয়্যার",
        "doctor": "ডাক্তার",
        "hospital": "হাসপাতাল",
        "police": "পুলিশ",
        "card": "কার্ড",
        "mobile": "মোবাইল",
        "phone": "ফোন",
        "office": "অফিস",
        "college": "কলেজ",
        "account": "অ্যাকাউন্ট",
        "credit": "ক্রেডিট",
        "debit": "ডেবিট",
        "laptop": "ল্যাপটপ",
        "computer": "কম্পিউটার",
        "internet": "ইন্টারনেট",
        "online": "অনলাইন"
    ]

    private init() {
        self.words = seedWords
        self.autodict = seedAutodict
        loadDictionary()
    }

    /// Load full dictionary from app bundle or resources
    public func loadDictionary() {
        queue.async { [weak self] in
            guard let self = self, !self.isLoaded else { return }

            var wordSet = self.seedWords
            var autoMap = self.seedAutodict

            // Check bundle path for words.txt
            let possibleWordsUrls = [
                Bundle.main.url(forResource: "words", withExtension: "txt"),
                Bundle.main.resourceURL?.appendingPathComponent("words.txt"),
                URL(fileURLWithPath: "/Library/Input Methods/Banglish.app/Contents/Resources/words.txt"),
                URL(fileURLWithPath: "\(NSHomeDirectory())/Library/Input Methods/Banglish.app/Contents/Resources/words.txt"),
                URL(fileURLWithPath: "/Users/rmacstudio2/Documents/Banglish/Resources/words.txt")
            ].compactMap { $0 }

            for url in possibleWordsUrls {
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

            // Check bundle path for autodict.txt
            let possibleAutoUrls = [
                Bundle.main.url(forResource: "autodict", withExtension: "txt"),
                Bundle.main.resourceURL?.appendingPathComponent("autodict.txt"),
                URL(fileURLWithPath: "/Library/Input Methods/Banglish.app/Contents/Resources/autodict.txt"),
                URL(fileURLWithPath: "\(NSHomeDirectory())/Library/Input Methods/Banglish.app/Contents/Resources/autodict.txt"),
                URL(fileURLWithPath: "/Users/rmacstudio2/Documents/Banglish/Resources/autodict.txt")
            ].compactMap { $0 }

            for url in possibleAutoUrls {
                if FileManager.default.fileExists(atPath: url.path) {
                    if let content = try? String(contentsOf: url, encoding: .utf8) {
                        content.enumerateLines { line, _ in
                            let parts = line.split(separator: "\t", maxSplits: 1).map(String.init)
                            if parts.count == 2 {
                                autoMap[parts[0].lowercased()] = parts[1]
                            }
                        }
                        break
                    }
                }
            }

            DispatchQueue.main.async {
                self.words = wordSet
                self.autodict = autoMap
                self.isLoaded = true
            }
        }
    }

    /// Check if word is valid in dictionary
    public func contains(_ word: String) -> Bool {
        return words.contains(word)
    }

    @inline(__always)
    private static func hasScalar(_ word: String, _ val: UInt32) -> Bool {
        return word.unicodeScalars.contains { $0.value == val }
    }

    @inline(__always)
    private static func replaceScalar(_ word: String, from: UInt32, to: UInt32) -> String {
        let newScalars = word.unicodeScalars.map { $0.value == from ? UnicodeScalar(to)! : $0 }
        return String(String.UnicodeScalarView(newScalars))
    }

    /// Generate ranked candidates for raw typed phonetic input
    public func candidates(for rawInput: String) -> [String] {
        guard !rawInput.isEmpty else { return [] }

        // Special handling for full stop / dot (.)
        if rawInput == "." {
            return ["।", "."]
        }

        let lower = rawInput.lowercased()
        let preferred = autodict[lower]

        let primary = BanglishEngine.shared.transliterate(rawInput)
        guard !primary.isEmpty else {
            if let pref = preferred { return [pref, rawInput] }
            return [rawInput]
        }

        var result: [String] = []
        var seen: Set<String> = []

        func addCandidate(_ word: String) {
            let trimmed = word.trimmingCharacters(in: .whitespacesAndNewlines)
            if !trimmed.isEmpty && !seen.contains(trimmed) {
                seen.insert(trimmed)
                result.append(trimmed)
            }
        }

        // 1. If we have a preferred standard spelling (e.g. "bank" -> "ব্যাংক", "iuai" -> "ইউআই"),
        // it MUST be candidate #1!
        if let pref = preferred {
            addCandidate(pref)
        }

        // 2. Direct engine transliteration is candidate #1 (or #2 if preferred exists)
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
            let hasHroshwoI = w.unicodeScalars.contains { $0.value == 0x09BF }
            let hasDeerghoI = w.unicodeScalars.contains { $0.value == 0x09C0 }
            if hasHroshwoI {
                let rep = w.unicodeScalars.map { $0.value == 0x09BF ? UnicodeScalar(0x09C0)! : $0 }
                variants.insert(String(String.UnicodeScalarView(rep)))
            }
            if hasDeerghoI {
                let rep = w.unicodeScalars.map { $0.value == 0x09C0 ? UnicodeScalar(0x09BF)! : $0 }
                variants.insert(String(String.UnicodeScalarView(rep)))
            }
        }

        // Rule C: ু <-> ূ (Hroshwo-U vs Deergho-U)
        let curWithU = Array(variants) + [primary]
        for w in curWithU {
            let hasHroshwoU = w.unicodeScalars.contains { $0.value == 0x09C1 }
            let hasDeerghoU = w.unicodeScalars.contains { $0.value == 0x09C2 }
            if hasHroshwoU {
                let rep = w.unicodeScalars.map { $0.value == 0x09C1 ? UnicodeScalar(0x09C2)! : $0 }
                variants.insert(String(String.UnicodeScalarView(rep)))
            }
            if hasDeerghoU {
                let rep = w.unicodeScalars.map { $0.value == 0x09C2 ? UnicodeScalar(0x09C1)! : $0 }
                variants.insert(String(String.UnicodeScalarView(rep)))
            }
        }

        // Rule D: স (0x09B8) <-> শ (0x09B6) <-> ষ (0x09B7)
        let curWithS = Array(variants) + [primary]
        for w in curWithS {
            if Self.hasScalar(w, 0x09B8) {
                variants.insert(Self.replaceScalar(w, from: 0x09B8, to: 0x09B6))
                variants.insert(Self.replaceScalar(w, from: 0x09B8, to: 0x09B7))
            }
            if Self.hasScalar(w, 0x09B6) {
                variants.insert(Self.replaceScalar(w, from: 0x09B6, to: 0x09B8))
                variants.insert(Self.replaceScalar(w, from: 0x09B6, to: 0x09B7))
            }
            if Self.hasScalar(w, 0x09B7) {
                variants.insert(Self.replaceScalar(w, from: 0x09B7, to: 0x09B6))
                variants.insert(Self.replaceScalar(w, from: 0x09B7, to: 0x09B8))
            }
        }

        // Rule E: ন (0x09A8) <-> ণ (0x09A3)
        let curWithN = Array(variants) + [primary]
        for w in curWithN {
            if Self.hasScalar(w, 0x09A8) {
                variants.insert(Self.replaceScalar(w, from: 0x09A8, to: 0x09A3))
            }
            if Self.hasScalar(w, 0x09A3) {
                variants.insert(Self.replaceScalar(w, from: 0x09A3, to: 0x09A8))
            }
        }

        // Rule F: র (0x09B0) <-> ড় (0x09DC) <-> ঢ় (0x09DD)
        let curWithR = Array(variants) + [primary]
        for w in curWithR {
            if Self.hasScalar(w, 0x09B0) {
                variants.insert(Self.replaceScalar(w, from: 0x09B0, to: 0x09DC))
            }
            if Self.hasScalar(w, 0x09DC) {
                variants.insert(Self.replaceScalar(w, from: 0x09DC, to: 0x09B0))
                variants.insert(Self.replaceScalar(w, from: 0x09DC, to: 0x09DD))
            }
            if Self.hasScalar(w, 0x09DD) {
                variants.insert(Self.replaceScalar(w, from: 0x09DD, to: 0x09DC))
            }
        }

        // Rule G: জ (0x099C) <-> য (0x09AF)
        let curWithJ = Array(variants) + [primary]
        for w in curWithJ {
            if Self.hasScalar(w, 0x099C) {
                variants.insert(Self.replaceScalar(w, from: 0x099C, to: 0x09AF))
            }
            if Self.hasScalar(w, 0x09AF) {
                variants.insert(Self.replaceScalar(w, from: 0x09AF, to: 0x099C))
            }
        }

        // Rule M: ছ (0x099B) <-> চ (0x099A) (Ch vs C ambiguity)
        let curWithCh = Array(variants) + [primary]
        for w in curWithCh {
            if Self.hasScalar(w, 0x099B) {
                variants.insert(Self.replaceScalar(w, from: 0x099B, to: 0x099A))
            }
            if Self.hasScalar(w, 0x099A) {
                variants.insert(Self.replaceScalar(w, from: 0x099A, to: 0x099B))
            }
        }

        // Rule N: Dental vs Retroflex (ত 0x09A4 <-> ট 0x099F)
        let curWithRetro = Array(variants) + [primary]
        for w in curWithRetro {
            if Self.hasScalar(w, 0x09A4) {
                variants.insert(Self.replaceScalar(w, from: 0x09A4, to: 0x099F))
            }
            if Self.hasScalar(w, 0x099F) {
                variants.insert(Self.replaceScalar(w, from: 0x099F, to: 0x09A4))
            }
        }

        // Rule O: Inherent vowel vs o-kar ambiguity (e.g. khoj -> খোজ, chok -> চোখ)
        if rawInput.contains("o") && !primary.contains("ো") {
            let withO = rawInput.replacingOccurrences(of: "o", with: "O")
            let transO = BanglishEngine.shared.transliterate(withO)
            if !transO.isEmpty {
                variants.insert(transO)
                if Self.hasScalar(transO, 0x099B) { variants.insert(Self.replaceScalar(transO, from: 0x099B, to: 0x099A)) }
                if Self.hasScalar(transO, 0x099A) { variants.insert(Self.replaceScalar(transO, from: 0x099A, to: 0x099B)) }
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
            if w.contains("ঙ্ক") { variants.insert(w.replacingOccurrences(of: "ঙ্ক", with: "ংক")) }
            if w.contains("ংক") { variants.insert(w.replacingOccurrences(of: "ংক", with: "ঙ্ক")) }
            if w.contains("জ্ঞ") { variants.insert(w.replacingOccurrences(of: "জ্ঞ", with: "গ্য")) }
            if w.contains("ক্ষ") { variants.insert(w.replacingOccurrences(of: "ক্ষ", with: "খ")) }
        }

        // Rule P: Y, Ja-fola (্য), Antostho-A (য়), Vowel (আই/ই) & W/War (ওয়্যার) variants
        // e.g. "iuai" -> "ইউয়াই" -> "ইউআই"
        // "softowar" / "software" -> "সফটওয়্যার"
        let curP = Array(variants) + [primary]
        for w in curP {
            if w.contains("্য") { variants.insert(w.replacingOccurrences(of: "্য", with: "য়")) }
            if w.contains("য়") { variants.insert(w.replacingOccurrences(of: "য়", with: "্য")) }
            if w.contains("য়াই") { variants.insert(w.replacingOccurrences(of: "য়াই", with: "আই")) }
            if w.contains("য়ি") { variants.insert(w.replacingOccurrences(of: "য়ি", with: "ই")) }
            if w.contains("ইয়া") { variants.insert(w.replacingOccurrences(of: "ইয়া", with: "িয়া")) }
            if w.contains("য়া") { variants.insert(w.replacingOccurrences(of: "য়া", with: "আ")) }
            if w.contains("তও") { variants.insert(w.replacingOccurrences(of: "তও", with: "ত্ব")) }
            if w.contains("ত্ব") { variants.insert(w.replacingOccurrences(of: "ত্ব", with: "তও")) }
            if w.contains("তওার") { variants.insert(w.replacingOccurrences(of: "তওার", with: "টওয়্যার")) }
            if w.contains("ত্বার") { variants.insert(w.replacingOccurrences(of: "ত্বার", with: "টওয়্যার")) }
            if w.contains("ওার") { variants.insert(w.replacingOccurrences(of: "ওার", with: "ওয়্যার")) }
            if w.contains("ও্যার") { variants.insert(w.replacingOccurrences(of: "ও্যার", with: "ওয়্যার")) }
            if w.contains("অ্যার") { variants.insert(w.replacingOccurrences(of: "অ্যার", with: "ওয়্যার")) }
            if w.contains("ওয়ার") { variants.insert(w.replacingOccurrences(of: "ওয়ার", with: "ওয়্যার")) }
            if w.contains("ফত") { variants.insert(w.replacingOccurrences(of: "ফত", with: "ফট")) }
        }

        // Rule Q: English loanwords with short 'a' (বা -> ব্যা, কা -> ক্যা, টা -> ট্যা, etc.) and 'nk' (বাঙ্ক -> ব্যাংক / ব্যাঙ্ক)
        let curQ = Array(variants) + [primary]
        for w in curQ {
            if w.contains("বাঙ্ক") {
                variants.insert(w.replacingOccurrences(of: "বাঙ্ক", with: "ব্যাংক"))
                variants.insert(w.replacingOccurrences(of: "বাঙ্ক", with: "ব্যাঙ্ক"))
            }
            if w.contains("টাঙ্ক") {
                variants.insert(w.replacingOccurrences(of: "টাঙ্ক", with: "ট্যাংক"))
                variants.insert(w.replacingOccurrences(of: "টাঙ্ক", with: "ট্যাঙ্ক"))
            }
            if w.contains("রাঙ্ক") {
                variants.insert(w.replacingOccurrences(of: "রাঙ্ক", with: "র‍্যাংক"))
            }
            if w.contains("লাঙ্ক") {
                variants.insert(w.replacingOccurrences(of: "লাঙ্ক", with: "লিংক"))
            }
            if w.hasPrefix("বা") && !w.hasPrefix("বাংলাদেশ") && !w.hasPrefix("বাংলা") {
                variants.insert("ব্যা" + String(w.dropFirst(2)))
            }
            if w.hasPrefix("কা") && !w.hasPrefix("কাজ") && !w.hasPrefix("কারণ") && !w.hasPrefix("কালো") {
                variants.insert("ক্যা" + String(w.dropFirst(2)))
            }
            if w.hasPrefix("টা") && !w.hasPrefix("টাকা") {
                variants.insert("ট্যা" + String(w.dropFirst(2)))
            }
            if w.hasPrefix("ফা") {
                variants.insert("ফ্যা" + String(w.dropFirst(2)))
            }
            if w.hasPrefix("গা") && !w.hasPrefix("গান") && !w.hasPrefix("গাছ") {
                variants.insert("গ্যা" + String(w.dropFirst(2)))
            }
        }

        // Rule J: Reph (র + Consonant -> র্ + Consonant) and Ro-fola (Consonant + র -> Consonant + ্র)
        let bConsonants = ["ক", "খ", "গ", "ঘ", "ঙ", "চ", "ছ", "জ", "ঝ", "ঞ", "ট", "ঠ", "ড", "ঢ", "ণ", "ত", "থ", "দ", "ধ", "ন", "প", "ফ", "ব", "ভ", "ম", "য", "ল", "শ", "ষ", "স", "হ", "ড়", "ঢ়", "য়"]
        let curWithReph = Array(variants) + [primary]
        for w in curWithReph {
            // Reph: র + C -> র্ + C (e.g. ডারক -> ডার্ক, করম -> কর্ম, বরন -> বর্ণ)
            for c in bConsonants {
                let target = "র" + c
                if w.contains(target) {
                    variants.insert(w.replacingOccurrences(of: target, with: "র্" + c))
                }
            }
            // Ro-fola: C + র -> C + ্র (e.g. ডরম -> ড্রাম / ড্রম, পরম -> প্রম, বরম -> ভ্রম)
            for c in bConsonants {
                if c == "র" || c == "ড়" || c == "ঢ়" || c == "ৎ" { continue }
                let target = c + "র"
                if w.contains(target) {
                    variants.insert(w.replacingOccurrences(of: target, with: c + "্র"))
                }
            }
        }

        // Rule K: Direct 'rr' reph variant for raw phonetic input (e.g. 'dark' -> 'darrk' -> 'ডার্ক')
        if rawInput.contains("r") && !rawInput.contains("rr") {
            let withDoubleR = rawInput.replacingOccurrences(of: "r", with: "rr")
            let transliteratedRR = BanglishEngine.shared.transliterate(withDoubleR)
            if !transliteratedRR.isEmpty {
                variants.insert(transliteratedRR)
            }
        }

        // Rule L: Comprehensive Chandrabindu (চন্দ্রবিন্দু ঁ) Suggestions
        let vowelSigns: Set<UInt32> = [
            0x09BE, // া
            0x09BF, // ি
            0x09C0, // ী
            0x09C1, // ু
            0x09C2, // ূ
            0x09C3, // ৃ
            0x09C7, // ে
            0x09C8, // ৈ
            0x09CB, // ো
            0x09CC  // ৌ
        ]
        let independentVowels: Set<UInt32> = [
            0x0985, 0x0986, 0x0987, 0x0988, 0x0989, 0x098A, 0x098B, 0x098F, 0x0990, 0x0993, 0x0994
        ]
        let nasalReplacements = [
            "ন্দ": "ঁদ",
            "ন্ত": "ঁত",
            "ঞ্চ": "ঁচ",
            "ম্প": "ঁপ",
            "ঙ্ক": "ঁক",
            "ন্স": "ঁস",
            "ঞ্জ": "ঁজ",
            "ন্ঠ": "ঁঠ",
            "ন্ড": "ঁড"
        ]

        var cbBaseWords = Array(variants) + [primary]

        for w in cbBaseWords {
            // 1. Insert ঁ after vowel signs or independent vowels (single-site substitution)
            let scalars = Array(w.unicodeScalars)
            for (i, sc) in scalars.enumerated() {
                if vowelSigns.contains(sc.value) || independentVowels.contains(sc.value) {
                    if i + 1 < scalars.count && scalars[i + 1].value == 0x0981 { continue }
                    var newScalars = scalars
                    newScalars.insert(UnicodeScalar(0x0981)!, at: i + 1)
                    let candidate = String(String.UnicodeScalarView(newScalars))
                    variants.insert(candidate)
                }
            }

            // 2. Nasal conjuncts to Chandrabindu
            for (from, to) in nasalReplacements {
                if w.contains(from) {
                    variants.insert(w.replacingOccurrences(of: from, with: to))
                }
            }
        }

        // 3. Raw phonetic input variations for Chandrabindu
        if !rawInput.contains("^") {
            let rawChars = Array(rawInput)
            for (i, ch) in rawChars.enumerated() {
                if "aeiouAEIOU".contains(ch) {
                    var newChars = rawChars
                    newChars.insert("^", at: i + 1)
                    let transliteratedCB = BanglishEngine.shared.transliterate(String(newChars))
                    if !transliteratedCB.isEmpty {
                        variants.insert(transliteratedCB)
                        if Self.hasScalar(transliteratedCB, 0x099B) {
                            variants.insert(Self.replaceScalar(transliteratedCB, from: 0x099B, to: 0x099A))
                        }
                        if Self.hasScalar(transliteratedCB, 0x099A) {
                            variants.insert(Self.replaceScalar(transliteratedCB, from: 0x099A, to: 0x099B))
                        }
                    }
                }
            }
        }

        // Prioritize dictionary confirmed variants:
        // Confirmed Chandrabindu words first, then other confirmed words
        var cbConfirmed: [String] = []
        var otherConfirmed: [String] = []
        var others: [String] = []

        for v in variants {
            if words.contains(v) {
                if v.unicodeScalars.contains(where: { $0.value == 0x0981 }) {
                    cbConfirmed.append(v)
                } else {
                    otherConfirmed.append(v)
                }
            } else {
                others.append(v)
            }
        }

        // Add confirmed Chandrabindu words first (after primary)
        for v in cbConfirmed {
            if result.count >= 8 { break }
            addCandidate(v)
        }

        // Add other confirmed dictionary words
        for v in otherConfirmed {
            if result.count >= 8 { break }
            addCandidate(v)
        }

        // If list still has room, add remaining phonetic variants (skip unconfirmed chandrabindu)
        for v in others {
            if result.count >= 7 { break }
            if v.contains("ঁ") && !words.contains(v) { continue }
            addCandidate(v)
        }

        // 3. Always append raw Latin text as the last candidate (exactly like reference)
        addCandidate(rawInput)

        return result
    }
}
