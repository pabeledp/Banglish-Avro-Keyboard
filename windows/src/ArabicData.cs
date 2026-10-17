using System;
using System.Collections.Generic;

namespace Banglish.Core
{
    public class ArabicPattern
    {
        public string Find { get; private set; }
        public string Replace { get; private set; }
        public bool IsDiacritic { get; private set; }

        public ArabicPattern(string find, string replace, bool isDiacritic = false)
        {
            Find = find;
            Replace = replace;
            IsDiacritic = isDiacritic;
        }
    }

    public static class ArabicData
    {
        public const string Fathatan    = "\u064B"; // ً
        public const string Dammatan    = "\u064C"; // ٌ
        public const string Kasratan    = "\u064D"; // ٍ
        public const string Fatha       = "\u064E"; // َ
        public const string Damma       = "\u064F"; // ُ
        public const string Kasra       = "\u0650"; // ِ
        public const string Shaddah     = "\u0651"; // ّ
        public const string Sukun       = "\u0652"; // ْ

        public const string Hamza       = "\u0621"; // ء
        public const string Alif        = "\u0627"; // ا
        public const string TaMarbuta   = "\u0629"; // ة
        public const string AlifMaqsura = "\u0649"; // ى
        public const string LamAlif     = "\u0644\u0627"; // لا

        public static readonly List<ArabicPattern> Patterns = new List<ArabicPattern>
        {
            // 1. Multi-character Harakat & Tanwin
            new ArabicPattern("shd", Shaddah, true),
            new ArabicPattern("an",  Fathatan, true),
            new ArabicPattern("un",  Dammatan, true),
            new ArabicPattern("in",  Kasratan, true),
            new ArabicPattern("sk",  Sukun, true),

            // 2. 2-character specials
            new ArabicPattern("9'", "\u0636"), // ض
            new ArabicPattern("6'", "\u0638"), // ظ
            new ArabicPattern("3'", "\u063A"), // غ
            new ArabicPattern("7'", "\u062E"), // خ
            new ArabicPattern("2'", "\u0623"), // أ

            new ArabicPattern("sh", "\u0634"), // ش
            new ArabicPattern("kh", "\u062E"), // خ
            new ArabicPattern("th", "\u062B"), // ث
            new ArabicPattern("gh", "\u063A"), // غ
            new ArabicPattern("dh", "\u0630"), // ذ

            new ArabicPattern("aa", "\u0627"), // ا
            new ArabicPattern("ee", "\u064A"), // ي
            new ArabicPattern("oo", "\u0648"), // و
            new ArabicPattern("ou", "\u0648"), // و
            new ArabicPattern("la", LamAlif),
            new ArabicPattern("lA", LamAlif),

            // 3. Arabizi numbers
            new ArabicPattern("2", Hamza),       // ء
            new ArabicPattern("3", "\u0639"),   // ع
            new ArabicPattern("5", "\u062E"),   // خ
            new ArabicPattern("6", "\u0637"),   // ط
            new ArabicPattern("7", "\u062D"),   // ح
            new ArabicPattern("8", "\u0642"),   // ق
            new ArabicPattern("9", "\u0635"),   // ص

            // 4. Single-character short harakat
            new ArabicPattern("a", Fatha, true),
            new ArabicPattern("u", Damma, true),
            new ArabicPattern("i", Kasra, true),
            new ArabicPattern("o", Damma, true),
            new ArabicPattern("e", Kasra, true),

            // 5. Root Consonants
            new ArabicPattern("A", Alif),
            new ArabicPattern("b", "\u0628"),
            new ArabicPattern("p", "\u0628"),
            new ArabicPattern("t", "\u062A"),
            new ArabicPattern("T", TaMarbuta),
            new ArabicPattern("j", "\u062C"),
            new ArabicPattern("g", "\u062C"),
            new ArabicPattern("d", "\u062F"),
            new ArabicPattern("r", "\u0631"),
            new ArabicPattern("z", "\u0632"),
            new ArabicPattern("s", "\u0633"),
            new ArabicPattern("f", "\u0641"),
            new ArabicPattern("v", "\u0641"),
            new ArabicPattern("q", "\u0642"),
            new ArabicPattern("k", "\u0643"),
            new ArabicPattern("c", "\u0643"),
            new ArabicPattern("l", "\u0644"),
            new ArabicPattern("m", "\u0645"),
            new ArabicPattern("n", "\u0646"),
            new ArabicPattern("h", "\u0647"),
            new ArabicPattern("H", "\u062D"),
            new ArabicPattern("w", "\u0648"),
            new ArabicPattern("y", "\u064A"),
            new ArabicPattern("Y", AlifMaqsura),

            // 6. Punctuation
            new ArabicPattern("?", "\u061F"),
            new ArabicPattern(",", "\u060C"),
            new ArabicPattern(";", "\u061B")
        };
    }
}
