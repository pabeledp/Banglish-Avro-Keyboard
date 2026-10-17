import Foundation

/// Arabizi (Franco-Arabic) Phonetic Mapping & Rule Definitions
public struct ArabicPattern {
    public let find: String
    public let replace: String
    public let isDiacritic: Bool

    public init(find: String, replace: String, isDiacritic: Bool = false) {
        self.find = find
        self.replace = replace
        self.isDiacritic = isDiacritic
    }
}

public struct ArabicData {
    // Diacritics (Harakat & Tanwin)
    public static let fathatan   = "\u{064B}" // ً
    public static let dammatan   = "\u{064C}" // ٌ
    public static let kasratan   = "\u{064D}" // ٍ
    public static let fatha      = "\u{064E}" // َ
    public static let damma      = "\u{064F}" // ُ
    public static let kasra      = "\u{0650}" // ِ
    public static let shaddah    = "\u{0651}" // ّ
    public static let sukun      = "\u{0652}" // ْ

    // Special Letters
    public static let hamza      = "\u{0621}" // ء
    public static let alif       = "\u{0627}" // ا
    public static let alifHamzaU = "\u{0623}" // أ
    public static let alifHamzaL = "\u{0625}" // إ
    public static let alifMadda  = "\u{0622}" // آ
    public static let taMarbuta  = "\u{0629}" // ة
    public static let alifMaqsura = "\u{0649}" // ى
    public static let lamAlif    = "\u{0644}\u{0627}" // لا

    /// All phonetic patterns sorted in descending order of length to ensure greedy matching
    public static let patterns: [ArabicPattern] = [
        // 1. Multi-character Harakat & Tanwin Shortcuts
        ArabicPattern(find: "shd", replace: shaddah, isDiacritic: true), // ّ Shaddah
        ArabicPattern(find: "an",  replace: fathatan, isDiacritic: true), // ً Fathatan
        ArabicPattern(find: "un",  replace: dammatan, isDiacritic: true), // ٌ Dammatan
        ArabicPattern(find: "in",  replace: kasratan, isDiacritic: true), // ٍ Kasratan
        ArabicPattern(find: "sk",  replace: sukun, isDiacritic: true),    // ْ Sukun

        // 2. 2-Character Special Letters & Digits with Apostrophe
        ArabicPattern(find: "9'", replace: "\u{0636}"), // ض (Dad)
        ArabicPattern(find: "6'", replace: "\u{0638}"), // ظ (Za)
        ArabicPattern(find: "3'", replace: "\u{063A}"), // غ (Ghayn)
        ArabicPattern(find: "7'", replace: "\u{062E}"), // خ (Kha alternative)
        ArabicPattern(find: "2'", replace: "\u{0623}"), // أ (Alif with Hamza)

        ArabicPattern(find: "sh", replace: "\u{0634}"), // ش (Shin)
        ArabicPattern(find: "kh", replace: "\u{062E}"), // خ (Kha)
        ArabicPattern(find: "th", replace: "\u{062B}"), // ث (Tha)
        ArabicPattern(find: "gh", replace: "\u{063A}"), // غ (Ghayn)
        ArabicPattern(find: "dh", replace: "\u{0630}"), // ذ (Dhal)
        ArabicPattern(find: "ch", replace: "\u{0686}"), // چ / تش

        // Long Vowels / Common Digraphs
        ArabicPattern(find: "aa", replace: "\u{0627}"), // ا (Long Alif)
        ArabicPattern(find: "ee", replace: "\u{064A}"), // ي (Long Ya)
        ArabicPattern(find: "oo", replace: "\u{0648}"), // و (Long Waw)
        ArabicPattern(find: "ou", replace: "\u{0648}"), // و (Waw)
        ArabicPattern(find: "la", replace: lamAlif),    // لا (Lam-Alif)
        ArabicPattern(find: "lA", replace: lamAlif),    // لا (Lam-Alif)

        // 3. Arabizi Digit Numbers
        ArabicPattern(find: "2", replace: hamza),       // ء (Hamza)
        ArabicPattern(find: "3", replace: "\u{0639}"), // ع (Ayn)
        ArabicPattern(find: "5", replace: "\u{062E}"), // خ (Kha)
        ArabicPattern(find: "6", replace: "\u{0637}"), // ط (Ta emphatic)
        ArabicPattern(find: "7", replace: "\u{062D}"), // ح (Ha pharyngeal)
        ArabicPattern(find: "8", replace: "\u{0642}"), // ق (Qaf)
        ArabicPattern(find: "9", replace: "\u{0635}"), // ص (Sad)

        // 4. Single-Character Short Harakat
        ArabicPattern(find: "a", replace: fatha, isDiacritic: true), // َ Fatha
        ArabicPattern(find: "u", replace: damma, isDiacritic: true), // ُ Damma
        ArabicPattern(find: "i", replace: kasra, isDiacritic: true), // ِ Kasra
        ArabicPattern(find: "o", replace: damma, isDiacritic: true), // ُ Damma fallback
        ArabicPattern(find: "e", replace: kasra, isDiacritic: true), // ِ Kasra fallback

        // 5. Root Consonants & Standard Letters
        ArabicPattern(find: "A", replace: alif),        // ا (Alif explicit)
        ArabicPattern(find: "b", replace: "\u{0628}"), // ب (Ba)
        ArabicPattern(find: "p", replace: "\u{0628}"), // ب / پ (Ba fallback)
        ArabicPattern(find: "t", replace: "\u{062A}"), // ت (Ta)
        ArabicPattern(find: "T", replace: taMarbuta),   // ة (Ta Marbuta explicit)
        ArabicPattern(find: "j", replace: "\u{062C}"), // ج (Jim)
        ArabicPattern(find: "g", replace: "\u{062C}"), // ج (Jim / Geem)
        ArabicPattern(find: "d", replace: "\u{062F}"), // د (Dal)
        ArabicPattern(find: "r", replace: "\u{0631}"), // ر (Ra)
        ArabicPattern(find: "z", replace: "\u{0632}"), // ز (Zay)
        ArabicPattern(find: "s", replace: "\u{0633}"), // س (Sin)
        ArabicPattern(find: "f", replace: "\u{0641}"), // ف (Fa)
        ArabicPattern(find: "v", replace: "\u{0641}"), // ف (Fa fallback)
        ArabicPattern(find: "q", replace: "\u{0642}"), // ق (Qaf)
        ArabicPattern(find: "k", replace: "\u{0643}"), // ك (Kaf)
        ArabicPattern(find: "c", replace: "\u{0643}"), // ك (Kaf fallback)
        ArabicPattern(find: "l", replace: "\u{0644}"), // ل (Lam)
        ArabicPattern(find: "m", replace: "\u{0645}"), // م (Mim)
        ArabicPattern(find: "n", replace: "\u{0646}"), // ن (Nun)
        ArabicPattern(find: "h", replace: "\u{0647}"), // ه (Ha)
        ArabicPattern(find: "H", replace: "\u{062D}"), // ح (Ha pharyngeal explicit)
        ArabicPattern(find: "w", replace: "\u{0648}"), // و (Waw)
        ArabicPattern(find: "y", replace: "\u{064A}"), // ي (Ya)
        ArabicPattern(find: "Y", replace: alifMaqsura),// ى (Alif Maqsura explicit)

        // 6. Punctuation
        ArabicPattern(find: "?", replace: "\u{061F}"), // ؟ (Arabic question mark)
        ArabicPattern(find: ",", replace: "\u{060C}"), // ، (Arabic comma)
        ArabicPattern(find: ";", replace: "\u{061B}"), // ؛ (Arabic semicolon)
    ]
}
