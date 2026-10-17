import Foundation

/// Fast Arabic Dictionary & Arabizi Phonetic Candidate Suggester
public final class ArabicDictionary {
    public static let shared = ArabicDictionary()

    private var autodict: [String: String] = [:]
    private let lock = NSLock()

    // Common seed Arabizi lexicon & high-frequency Arabic words
    private let seedAutodict: [String: String] = [
        // Greetings & Etiquette
        "salam": "سلام",
        "alsalam": "السلام",
        "marhaba": "مرحبا",
        "mar7aba": "مرحبا",
        "ahlan": "أهلا",
        "shukran": "شكرا",
        "shokran": "شكرا",
        "afwan": "عفوا",
        "3afwan": "عفوا",
        "habibi": "حبيبي",
        "7abibi": "حبيبي",
        "habibti": "حبيبتي",
        "7abibti": "حبيبتي",
        "tamam": "تمام",
        "kwayes": "كويس",
        "mnih": "منيح",
        "mnee7": "منيح",
        "khalas": "خلاص",
        "5alas": "خلاص",
        "yalla": "يلا",
        "aywa": "أيوه",
        "na3am": "نعم",
        "naam": "نعم",
        "la": "لا",
        "laa": "لا",

        // Islamic & Cultural Phrases
        "inshallah": "إن شاء الله",
        "inshaallah": "إن شاء الله",
        "mashallah": "ما شاء الله",
        "mashaallah": "ما شاء الله",
        "alhamdulillah": "الحمد لله",
        "elhamdulillah": "الحمد لله",
        "subhanallah": "سبحان الله",
        "astaghfirullah": "أستغفر الله",
        "bismillah": "بسم الله",
        "allah": "الله",
        "mohammad": "محمد",
        "muhammad": "محمد",
        "quran": "قرآن",
        "8oran": "قرآن",
        "islam": "إسلام",
        "muslim": "مسلم",
        "masjid": "مسجد",
        "jumaa": "جمعة",
        "jum3a": "جمعة",
        "eid": "عيد",
        "ramadan": "رمضان",

        // Common Questions & Pronouns
        "kayf": "كيف",
        "kef": "كيف",
        "kaifa": "كيف",
        "kayfak": "كيفك",
        "kefak": "كيفك",
        "wen": "وين",
        "wayn": "أين",
        "ayna": "أين",
        "lesh": "ليش",
        "lemadha": "لماذا",
        "meen": "مين",
        "man": "من",
        "shu": "شو",
        "matha": "ماذا",
        "kam": "كم",
        "mata": "متى",
        "ana": "أنا",
        "enta": "أنت",
        "enti": "أنتِ",
        "howa": "هو",
        "hiya": "هي",
        "ne7na": "نحن",
        "nahnu": "نحن",
        "hom": "هم",

        // Everyday Vocabulary
        "kitab": "كتاب",
        "qalam": "قلم",
        "8alam": "قلم",
        "bayt": "بيت",
        "beit": "بيت",
        "madrasa": "مدرسة",
        "jami3a": "جامعة",
        "shams": "شمس",
        "qamar": "قمر",
        "yawm": "يوم",
        "yom": "يوم",
        "shahr": "شهر",
        "sana": "سنة",
        "waqt": "وقت",
        "sa3a": "ساعة",
        "sooq": "سوق",
        "souq": "سوق",
        "mat3am": "مطعم",
        "mataam": "مطعم",
        "funduq": "فندق",
        "sayyara": "سيارة",
        "tayyara": "طيارة",
        "matar": "مطار",
        "qahwa": "قهوة",
        "8ahwa": "قهوة",
        "shay": "شاي",
        "ma": "ماء",
        "moya": "مويه",
        "khobz": "خبز",
        "5obz": "خبز",
        "akl": "أكل",
        "ful": "فول",
        "kabab": "كباب",
        "3arab": "عرب",
        "arab": "عرب",
        "3arabi": "عربي",
        "arabi": "عربي",
        "lugha": "لغة"
    ]

    private init() {
        self.autodict = seedAutodict
    }

    /// Generate ranked candidates for Arabizi input
    public func candidates(for rawInput: String) -> [String] {
        guard !rawInput.isEmpty else { return [] }

        let lower = rawInput.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        var preferred: String? = nil
        lock.lock()
        preferred = autodict[lower]
        lock.unlock()

        let primary = ArabicEngine.shared.transliterate(rawInput)
        var result: [String] = []
        var seen: Set<String> = []

        func addCandidate(_ word: String) {
            let trimmed = word.trimmingCharacters(in: .whitespacesAndNewlines)
            if !trimmed.isEmpty && !seen.contains(trimmed) {
                seen.insert(trimmed)
                result.append(trimmed)
            }
        }

        // 1. High-frequency dictionary match
        if let pref = preferred {
            addCandidate(pref)
        }

        // 2. Primary phonetic transliteration
        if !primary.isEmpty {
            addCandidate(primary)

            // 3. Diacritic-stripped version (so users easily get unvocalized Arabic)
            let stripped = ArabicEngine.shared.stripDiacritics(primary)
            if stripped != primary {
                addCandidate(stripped)
            }

            // 4. Common Arabic orthographic variations
            // Ta Marbuta (ة) <-> Ha (ه)
            if primary.hasSuffix("ة") {
                addCandidate(String(primary.dropLast()) + "ه")
            } else if primary.hasSuffix("ه") {
                addCandidate(String(primary.dropLast()) + "ة")
            }

            // Alif Maqsura (ى) <-> Ya (ي)
            if primary.hasSuffix("ى") {
                addCandidate(String(primary.dropLast()) + "ي")
            } else if primary.hasSuffix("ي") {
                addCandidate(String(primary.dropLast()) + "ى")
            }

            // Alif with Hamza (أ / إ) <-> Bare Alif (ا)
            if primary.hasPrefix("أ") || primary.hasPrefix("إ") {
                addCandidate("ا" + String(primary.dropFirst()))
            }
        }

        return result
    }
}
