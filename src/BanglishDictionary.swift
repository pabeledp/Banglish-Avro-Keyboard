import Foundation

/// Fast Bengali Dictionary & Phonetic Candidate Suggester
public final class BanglishDictionary {
    public static let shared = BanglishDictionary()

    private var words: Set<String> = []
    private var autodict: [String: String] = [:]
    private var isLoaded = false
    private let lock = NSLock()
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
        "ব্যাংক", "ব্যাঙ্ক", "ইউআই", "ট্যাংক", "সফটওয়্যার", "ভালো", "ভাল", "কেমন", "আছো", "আছেন", "ধন্যবাদ", "স্বাগতম",
        "চাঁদ", "হাঁস", "বাঁধা", "খোঁজ", "ডাক্তার", "সুন্দর", "নির্ভুল", "প্রবলেম",
        "ডিভাইস", "আপডেট", "কিবোর্ড", "সাজেশন", "প্লেসমেন্ট", "করব", "করবে", "করো",
        "হবে", "সময়", "সবার", "কিছু", "সবচেয়ে", "লেখা", "শব্দ", "নতুন", "সব",
        "কাজ", "ঠিক", "আসছে", "আসছিল", "হচ্ছে", "হচ্ছিল", "কিন্তু", "কথা", "কোথায়",
        "কোথাও", "বাইরে", "ভেতরে", "ভিতরে", "এখন", "আগে", "পরে", "দূরত্ব", "নিরাপদ",
        "হওয়া", "হওয়ার", "হওয়াতে", "হওয়ায়", "খাওয়া", "খাওয়ার", "দেওয়া", "দেওয়ার",
        "যাওয়া", "যাওয়ার", "পাওয়া", "পাওয়ার", "নেওয়া", "নেওয়ার", "চাওয়া", "চাওয়ার",
        "ধোয়া", "ধোওয়া", "শোওয়া", "শোয়া",
        "এমন", "এমনি", "এমনই", "যেমন", "যেমনি", "যেমনই", "তেমন", "তেমনি", "তেমনই", "কেমন", "কেমনি", "কেমনই",
        "আস্তে", "আসতে", "তখন", "তখনই", "তখনি", "এখনই", "এখনি",
        "চোখ", "চোখে", "চোখের", "লোক", "লোকে", "লোকের", "রোগ", "রোগে", "রোগের", "রোগী",
        "বলো", "ছোট", "বড়ো", "বড়", "শোনো", "দরজা", "খবর", "পাঁচ"
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
        "daktar": "ডাক্তার",
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
        "online": "অনলাইন",
        "chad": "চাঁদ",
        "has": "হাঁস",
        "badha": "বাঁধা",
        "khuj": "খোঁজ",
        "khoj": "খোঁজ",
        "shundor": "সুন্দর",
        "sundor": "সুন্দর",
        "dhonnobad": "ধন্যবাদ",
        "nirvul": "নির্ভুল",
        "problem": "প্রবলেম",
        "device": "ডিভাইস",
        "update": "আপডেট",
        "keyboard": "কিবোর্ড",
        "suggestion": "সাজেশন",
        "placement": "প্লেসমেন্ট",
        "bhalo": "ভালো",
        "valo": "ভালো",
        "kemon": "কেমন",
        "achen": "আছেন",
        "korbo": "করব",
        "korbe": "করবে",
        "koro": "করো",
        "hobe": "হবে",
        "somoy": "সময়",
        "sobar": "সবার",
        "kichu": "কিছু",
        "shobcheye": "সবচেয়ে",
        "sobcheye": "সবচেয়ে",
        "nirapod": "নিরাপদ",
        "durotto": "দূরত্ব"
    ]

    private init() {
        self.words = seedWords
        self.autodict = seedAutodict
        loadDictionary()
    }

    /// Load full dictionary from app bundle or resources
    public func loadDictionary() {
        queue.async { [weak self] in
            guard let self = self else { return }
            self.lock.lock()
            if self.isLoaded {
                self.lock.unlock()
                return
            }
            self.lock.unlock()

            var wordSet = self.seedWords
            var autoMap = self.seedAutodict

            // Check bundle and local paths for words.txt
            let possibleWordsUrls = [
                Bundle.main.url(forResource: "words", withExtension: "txt"),
                Bundle.main.resourceURL?.appendingPathComponent("words.txt"),
                Bundle(for: BanglishDictionary.self).url(forResource: "words", withExtension: "txt"),
                URL(fileURLWithPath: "/Library/Input Methods/Banglish.app/Contents/Resources/words.txt"),
                URL(fileURLWithPath: "\(NSHomeDirectory())/Library/Input Methods/Banglish.app/Contents/Resources/words.txt"),
                URL(fileURLWithPath: "Resources/words.txt"),
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

            // Check bundle and local paths for autodict.txt
            let possibleAutoUrls = [
                Bundle.main.url(forResource: "autodict", withExtension: "txt"),
                Bundle.main.resourceURL?.appendingPathComponent("autodict.txt"),
                Bundle(for: BanglishDictionary.self).url(forResource: "autodict", withExtension: "txt"),
                URL(fileURLWithPath: "/Library/Input Methods/Banglish.app/Contents/Resources/autodict.txt"),
                URL(fileURLWithPath: "\(NSHomeDirectory())/Library/Input Methods/Banglish.app/Contents/Resources/autodict.txt"),
                URL(fileURLWithPath: "Resources/autodict.txt"),
                URL(fileURLWithPath: "/Users/rmacstudio2/Documents/Banglish/Resources/autodict.txt")
            ].compactMap { $0 }

            for url in possibleAutoUrls {
                if FileManager.default.fileExists(atPath: url.path) {
                    if let content = try? String(contentsOf: url, encoding: .utf8) {
                        content.enumerateLines { line, _ in
                            let parts = line.split(separator: "\t", maxSplits: 1).map(String.init)
                            if parts.count == 2 {
                                autoMap[parts[0].trimmingCharacters(in: .whitespacesAndNewlines).lowercased()] = parts[1].trimmingCharacters(in: .whitespacesAndNewlines)
                            }
                        }
                        break
                    }
                }
            }

            self.lock.lock()
            self.words = wordSet
            self.autodict = autoMap
            self.isLoaded = true
            self.lock.unlock()
        }
    }

    /// Check if word is valid in dictionary
    public func contains(_ word: String) -> Bool {
        lock.lock()
        defer { lock.unlock() }
        return words.contains(word)
    }

    private static func replacePrefixScalars(_ str: String, prefix: [UInt32], replacement: [UInt32]) -> String? {
        let sc = Array(str.unicodeScalars.map { $0.value })
        if sc.starts(with: prefix) {
            let newSc = replacement + sc.dropFirst(prefix.count)
            return String(String.UnicodeScalarView(newSc.compactMap { UnicodeScalar($0) }))
        }
        return nil
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

        // Numbers (0-9) do not need floating candidate popups
        if rawInput.allSatisfy({ $0.isNumber }) {
            return []
        }

        // Special handling for full stop / dot (.)
        if rawInput == "." {
            return ["।", "."]
        }

        let lower = rawInput.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        var preferred: String? = nil
        lock.lock()
        preferred = autodict[lower]
        lock.unlock()

        var primary = BanglishEngine.shared.transliterate(rawInput)
        guard !primary.isEmpty else {
            if let pref = preferred { return [pref, rawInput] }
            return [rawInput]
        }
        if primary.contains("ওা") {
            primary = primary.replacingOccurrences(of: "ওা", with: "ওয়া")
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

        // Rule O: o-kar ambiguity (chok -> চোখ, lok -> লোক, rog -> রোগ, koro -> করো, bolo -> বলো, choto -> ছোট, boro -> বড়/বড়ো, kothao -> কোথাও)
        if rawInput.contains("o") {
            let allO = BanglishEngine.shared.transliterate(rawInput.replacingOccurrences(of: "o", with: "O"))
            if !allO.isEmpty { variants.insert(allO) }

            if let lastO = rawInput.lastIndex(of: "o") {
                var lastOStr = rawInput
                lastOStr.replaceSubrange(lastO...lastO, with: "O")
                let transLastO = BanglishEngine.shared.transliterate(lastOStr)
                if !transLastO.isEmpty { variants.insert(transLastO) }
            }

            if let firstO = rawInput.firstIndex(of: "o") {
                var firstOStr = rawInput
                firstOStr.replaceSubrange(firstO...firstO, with: "O")
                let transFirstO = BanglishEngine.shared.transliterate(firstOStr)
                if !transFirstO.isEmpty { variants.insert(transFirstO) }
            }
        }

        // Rule W: wa / wya / oa -> ওয়া normalization (howa/howya -> হওয়া, khawa -> খাওয়া, dewa -> দেওয়া, jawa -> যাওয়া, pawa -> পাওয়া)
        let curW = Array(variants) + [primary]
        for w in curW {
            if w.contains("ওা") { variants.insert(w.replacingOccurrences(of: "ওা", with: "ওয়া")) }
        }

        // Rule MN: Consonant shorthand expansion (emn -> এমন/এম্ন, kmn -> কেমন/কম্ন, tmn -> তেমন/তম্ন, jmn -> যেমন/জম্ন, amn -> এমন/আমন)
        let curMN = Array(variants) + [primary]
        for w in curMN {
            if w.contains("ম্ন") { variants.insert(w.replacingOccurrences(of: "ম্ন", with: "মন")) }
        }
        if rawInput.lowercased().hasSuffix("mn") {
            let baseStem = String(rawInput.dropLast(2))
            let withO = BanglishEngine.shared.transliterate(baseStem + "on")
            if !withO.isEmpty { variants.insert(withO) }
            let withMon = BanglishEngine.shared.transliterate(baseStem + "mon")
            if !withMon.isEmpty { variants.insert(withMon) }

            if baseStem.count == 1 {
                let withE = BanglishEngine.shared.transliterate(baseStem + "emon")
                if !withE.isEmpty { variants.insert(withE) }
            }
        }

        // Rule ST: Conjunct vs Non-conjunct (aste -> আস্তে & আসতে, boste -> বস্তে & বসতে, haste -> হস্তে & হাসতে)
        let curST = Array(variants) + [primary]
        for w in curST {
            if w.contains("স্ত") { variants.insert(w.replacingOccurrences(of: "স্ত", with: "সত")) }
        }
        if rawInput.contains("ste") {
            let modST = rawInput.replacingOccurrences(of: "ste", with: "s`te")
            let transST = BanglishEngine.shared.transliterate(modST)
            if !transST.isEmpty { variants.insert(transST) }
        }

        // Rule NI: Emphatic suffix ni <-> noi (jemoni -> যেমনই & যেমনি, temoni -> তেমনই & তেমনি, emoni -> এমনই & এমনি, kemoni -> কেমনই & কেমনি)
        let curNI = Array(variants) + [primary]
        for w in curNI {
            if w.hasSuffix("নি") { variants.insert(String(w.dropLast(1)) + "নই") }
            if w.hasSuffix("নই") { variants.insert(String(w.dropLast(2)) + "নি") }
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
            if w.contains("বাঙ্ক") {
                variants.insert(w.replacingOccurrences(of: "বাঙ্ক", with: "ব্যাংক"))
                variants.insert(w.replacingOccurrences(of: "বাঙ্ক", with: "ব্যাঙ্ক"))
            }
            if w.contains("বাংক") {
                variants.insert(w.replacingOccurrences(of: "বাংক", with: "ব্যাংক"))
                variants.insert(w.replacingOccurrences(of: "বাংক", with: "ব্যাঙ্ক"))
            }
            if w.contains("টাঙ্ক") {
                variants.insert(w.replacingOccurrences(of: "টাঙ্ক", with: "ট্যাংক"))
                variants.insert(w.replacingOccurrences(of: "টাঙ্ক", with: "ট্যাঙ্ক"))
            }
            if w.contains("টাংক") {
                variants.insert(w.replacingOccurrences(of: "টাংক", with: "ট্যাংক"))
                variants.insert(w.replacingOccurrences(of: "টাংক", with: "ট্যাঙ্ক"))
            }
            if w.contains("রাঙ্ক") { variants.insert(w.replacingOccurrences(of: "রাঙ্ক", with: "র‍্যাংক")) }
            if w.contains("রাংক") { variants.insert(w.replacingOccurrences(of: "রাংক", with: "র‍্যাংক")) }
            if w.contains("লাঙ্ক") { variants.insert(w.replacingOccurrences(of: "লাঙ্ক", with: "লিংক")) }
            if w.contains("লাংক") { variants.insert(w.replacingOccurrences(of: "লাংক", with: "লিংক")) }
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
            if w.contains("ত্বারে") { variants.insert(w.replacingOccurrences(of: "ত্বারে", with: "টওয়্যার")) }
            if w.contains("ওার") { variants.insert(w.replacingOccurrences(of: "ওার", with: "ওয়্যার")) }
            if w.contains("ও্যার") { variants.insert(w.replacingOccurrences(of: "ও্যার", with: "ওয়্যার")) }
            if w.contains("অ্যার") { variants.insert(w.replacingOccurrences(of: "অ্যার", with: "ওয়্যার")) }
            if w.contains("ওয়ার") { variants.insert(w.replacingOccurrences(of: "ওয়ার", with: "ওয়্যার")) }
            if w.contains("ফত") { variants.insert(w.replacingOccurrences(of: "ফত", with: "ফট")) }
        }

        // Also check phonetic transliteration variations of rawInput for 'y' and 'w'
        if rawInput.contains("y") {
            // 'y' as 'Y' (forces য়)
            let transY = BanglishEngine.shared.transliterate(rawInput.replacingOccurrences(of: "y", with: "Y"))
            if !transY.isEmpty { variants.insert(transY) }
            // 'y' as 'Z' (forces ্য)
            let transZ = BanglishEngine.shared.transliterate(rawInput.replacingOccurrences(of: "y", with: "Z"))
            if !transZ.isEmpty { variants.insert(transZ) }
        }
        if rawInput.contains("war") || rawInput.contains("wer") || rawInput.contains("ware") {
            let modInput = rawInput
                .replacingOccurrences(of: "software", with: "softwZar")
                .replacingOccurrences(of: "softowar", with: "softwZar")
                .replacingOccurrences(of: "softwer", with: "softwZar")
                .replacingOccurrences(of: "ware", with: "wZar")
            let transW = BanglishEngine.shared.transliterate(modInput)
            if !transW.isEmpty {
                variants.insert(transW)
                if transW.contains("ফত") {
                    variants.insert(transW.replacingOccurrences(of: "ফত", with: "ফট"))
                }
            }
        }

        // Rule Q: English loanwords with short 'a' (বা -> ব্যা, কা -> ক্যা, টা -> ট্যা, etc.)
        let prefixReplacements: [([UInt32], [UInt32], [String])] = [
            ([0x09AC, 0x09BE], [0x09AC, 0x09CD, 0x09AF, 0x09BE], ["বাংলাদেশ", "বাংলা", "বাঙলা", "বাগান", "বাতাস", "বাড়ি", "বাড়ি", "বাপ"]),
            ([0x0995, 0x09BE], [0x0995, 0x09CD, 0x09AF, 0x09BE], ["কাজ", "কারণ", "কারন", "কালো", "কাল", "কাছে", "কাগজ"]),
            ([0x099F, 0x09BE], [0x099F, 0x09CD, 0x09AF, 0x09BE], ["টাকা", "টাক"]),
            ([0x0997, 0x09BE], [0x0997, 0x09CD, 0x09AF, 0x09BE], ["গান", "গাছ", "গায়ে", "গা"]),
            ([0x09AB, 0x09BE], [0x09AB, 0x09CD, 0x09AF, 0x09BE], [])
        ]

        let curQ = Array(variants) + [primary]
        for w in curQ {
            for (pfx, repl, exceptions) in prefixReplacements {
                var isException = false
                for ex in exceptions {
                    if w.hasPrefix(ex) { isException = true; break }
                }
                if !isException, let mod = Self.replacePrefixScalars(w, prefix: pfx, replacement: repl) {
                    variants.insert(mod)
                }
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

        let cbBaseWords = Array(variants) + [primary]
        for w in cbBaseWords {
            // Nasal conjuncts to Chandrabindu (e.g. chad -> চাঁদ, has -> হাঁস, badha -> বাঁধা)
            for (from, to) in nasalReplacements {
                if w.contains(from) {
                    variants.insert(w.replacingOccurrences(of: from, with: to))
                }
            }
        }

        // Prioritize dictionary confirmed variants:
        // Confirmed Chandrabindu words first, then other confirmed words
        var cbConfirmed: [String] = []
        var otherConfirmed: [String] = []
        var others: [String] = []

        lock.lock()
        for v in variants {
            let hasCB = v.unicodeScalars.contains { $0.value == 0x0981 }
            if words.contains(v) {
                if hasCB {
                    cbConfirmed.append(v)
                } else {
                    otherConfirmed.append(v)
                }
            } else {
                // Do not allow unconfirmed fabricated Chandrabindu words
                if !hasCB {
                    others.append(v)
                }
            }
        }
        lock.unlock()

        let primaryConfirmed = self.contains(primary)

        let userExplicitCB = rawInput.contains("^")

        if primaryConfirmed {
            // Primary transliteration is an authenticated dictionary word (highest accuracy)
            addCandidate(primary)

            if userExplicitCB {
                for v in cbConfirmed { if result.count >= 8 { break }; addCandidate(v) }
                for v in otherConfirmed { if result.count >= 8 { break }; addCandidate(v) }
            } else {
                for v in otherConfirmed { if result.count >= 8 { break }; addCandidate(v) }
                for v in cbConfirmed { if result.count >= 8 { break }; addCandidate(v) }
            }
        } else {
            // Primary is unconfirmed, prioritize verified accurate dictionary spellings
            if userExplicitCB {
                for v in cbConfirmed { if result.count >= 8 { break }; addCandidate(v) }
                for v in otherConfirmed { if result.count >= 8 { break }; addCandidate(v) }
            } else {
                for v in otherConfirmed { if result.count >= 8 { break }; addCandidate(v) }
                for v in cbConfirmed { if result.count >= 8 { break }; addCandidate(v) }
            }
            // Add unconfirmed primary transliteration as fallback
            addCandidate(primary)
        }

        // If list still has room, add remaining phonetic variants
        for v in others {
            if result.count >= 7 { break }
            addCandidate(v)
        }

        // 3. Always append raw Latin text as the last candidate (exactly like reference)
        addCandidate(rawInput)

        return result
    }
}
