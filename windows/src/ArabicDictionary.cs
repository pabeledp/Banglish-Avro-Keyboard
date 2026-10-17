using System;
using System.Collections.Generic;

namespace Banglish.Core
{
    public class ArabicDictionary
    {
        public static readonly ArabicDictionary Shared = new ArabicDictionary();

        private readonly Dictionary<string, string> _autodict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "salam", "سلام" },
            { "alsalam", "السلام" },
            { "marhaba", "مرحبا" },
            { "mar7aba", "مرحبا" },
            { "ahlan", "أهلا" },
            { "shukran", "شكرا" },
            { "shokran", "شكرا" },
            { "afwan", "عفوا" },
            { "3afwan", "عفوا" },
            { "habibi", "حبيبي" },
            { "7abibi", "حبيبي" },
            { "habibti", "حبيبتي" },
            { "7abibti", "حبيبتي" },
            { "tamam", "تمام" },
            { "kwayes", "كويس" },
            { "mnih", "منيح" },
            { "mnee7", "منيح" },
            { "khalas", "خلاص" },
            { "5alas", "خلاص" },
            { "yalla", "يلا" },
            { "aywa", "أيوه" },
            { "na3am", "نعم" },
            { "naam", "نعم" },
            { "la", "لا" },
            { "laa", "لا" },
            { "inshallah", "إن شاء الله" },
            { "inshaallah", "إن شاء الله" },
            { "mashallah", "ما شاء الله" },
            { "mashaallah", "ما شاء الله" },
            { "alhamdulillah", "الحمد لله" },
            { "elhamdulillah", "الحمد لله" },
            { "subhanallah", "سبحان الله" },
            { "astaghfirullah", "أستغفر الله" },
            { "bismillah", "بسم الله" },
            { "allah", "الله" },
            { "mohammad", "محمد" },
            { "muhammad", "محمد" },
            { "quran", "قرآن" },
            { "8oran", "قرآن" },
            { "islam", "إسلام" },
            { "muslim", "مسلم" },
            { "masjid", "مسجد" },
            { "kayf", "كيف" },
            { "kef", "كيف" },
            { "kaifa", "كيف" },
            { "kayfak", "كيفك" },
            { "kefak", "كيفك" },
            { "kitab", "كتاب" },
            { "qalam", "قلم" },
            { "8alam", "قلم" },
            { "bayt", "بيت" },
            { "beit", "بيت" },
            { "madrasa", "مدرسة" },
            { "jami3a", "جامعة" },
            { "shams", "شمس" },
            { "qamar", "قمر" },
            { "yawm", "يوم" },
            { "yom", "يوم" },
            { "shahr", "شهر" },
            { "sana", "سنة" },
            { "waqt", "وقت" },
            { "sa3a", "ساعة" },
            { "sooq", "سوق" },
            { "souq", "سوق" },
            { "mat3am", "مطعم" },
            { "mataam", "مطعم" },
            { "funduq", "فندق" },
            { "sayyara", "سيارة" },
            { "tayyara", "طيارة" },
            { "matar", "مطار" },
            { "qahwa", "قهوة" },
            { "8ahwa", "قهوة" },
            { "shay", "شاي" },
            { "ma", "ماء" },
            { "khobz", "خبز" },
            { "5obz", "خبز" },
            { "ful", "فول" },
            { "kabab", "كباب" },
            { "3arab", "عرب" },
            { "arab", "عرب" },
            { "3arabi", "عربي" },
            { "arabi", "عربي" },
            { "lugha", "لغة" }
        };

        public List<string> Candidates(string rawInput)
        {
            if (string.IsNullOrEmpty(rawInput)) return new List<string>();

            string lower = rawInput.Trim().ToLowerInvariant();
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>();

            void Add(string word)
            {
                if (!string.IsNullOrEmpty(word) && !seen.Contains(word))
                {
                    seen.Add(word);
                    result.Add(word);
                }
            }

            if (_autodict.TryGetValue(lower, out string pref))
            {
                Add(pref);
            }

            string primary = ArabicEngine.Shared.Transliterate(rawInput);
            if (!string.IsNullOrEmpty(primary))
            {
                Add(primary);
                string stripped = ArabicEngine.Shared.StripDiacritics(primary);
                if (stripped != primary)
                {
                    Add(stripped);
                }

                if (primary.EndsWith("ة")) Add(primary.Substring(0, primary.Length - 1) + "ه");
                else if (primary.EndsWith("ه")) Add(primary.Substring(0, primary.Length - 1) + "ة");

                if (primary.EndsWith("ى")) Add(primary.Substring(0, primary.Length - 1) + "ي");
                else if (primary.EndsWith("ي")) Add(primary.Substring(0, primary.Length - 1) + "ى");

                if (primary.StartsWith("أ") || primary.StartsWith("إ"))
                {
                    Add("ا" + primary.Substring(1));
                }
            }

            return result;
        }
    }
}
