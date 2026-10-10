using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Banglish.Core
{
    public sealed class BanglishDictionary
    {
        private static readonly string[] SeedWords = new string[]
        {
            "আমি", "তুমি", "আমরা", "তোমরা", "তারা", "সে", "আপনি", "আপনারা",
            "বাংলাদেশ", "বাংলা", "বাঙলা", "বাংলিশ", "ভাষায়", "ভাষা", "ভাষার",
            "বাজিমাত", "বাজিমাৎ", "বাজীমাত", "বাজীমাৎ", "বাজিকর",
            "শান্তি", "শান্তী", "সান্তি", "আনন্দ", "আনন্দময়", "বন্ধু", "বন্ধুরা",
            "যুক্তবর্ণ", "যুক্ত", "বর্ণ", "জ্ঞান", "বিজ্ঞান", "বিজ্ঞানী", "স্বাধীনতা",
            "ইতিহাস", "পরীক্ষা", "বৃষ্টি", "সন্ধ্যা", "কঠিন", "কঠীন", "খুব", "খূব",
            "পানি", "পানী", "নদী", "নদি", "সূর্য", "সুর্য", "কারণ", "কারন",
            "মানুষ", "মানুস", "দেশ", "দেশের", "বিপদ", "বিপদ্", "হঠাৎ", "হঠাত",
            "সৃষ্টি", "সৃষ্টী", "অনুষ্ঠান", "পুষ্প", "অঙ্ক", "অংক", "সঙ্গ", "সংগ",
            "ভালো", "ভাল", "কেমন", "আছো", "আছেন", "ধন্যবাদ", "স্বাগতম",
            "হাফেজ", "হাফেয", "নামাজ", "নামায", "কাগজ", "কাগয", "জাহাজ", "জাহায",
            "ইউআই", "জিইউআই", "সফটওয়্যার", "হার্ডওয়্যার", "ফার্মওয়্যার", "ওয়েবসাইট", "কম্পিউটার"
        };

        public static readonly BanglishDictionary Shared = new BanglishDictionary();

        private readonly HashSet<string> _words = new HashSet<string>(StringComparer.Ordinal);
        private readonly object _lock = new object();
        private bool _isLoaded = false;

        private static readonly string[] BengaliConsonants = new string[]
        {
            "ক", "খ", "গ", "ঘ", "ঙ", "চ", "ছ", "জ", "ঝ", "ঞ", "ট", "ঠ", "ড", "ঢ", "ণ",
            "ত", "থ", "দ", "ধ", "ন", "প", "ফ", "ব", "ভ", "ম", "য", "ল", "শ", "ষ", "স",
            "হ", "ড়", "ঢ়", "য়"
        };

        private struct NasalPair
        {
            public string From;
            public string To;
            public NasalPair(string from, string to)
            {
                From = from;
                To = to;
            }
        }

        private static readonly NasalPair[] NasalPairs = new NasalPair[]
        {
            new NasalPair("ন্দ", "ঁদ"), new NasalPair("ন্ত", "ঁত"), new NasalPair("ঞ্চ", "ঁচ"),
            new NasalPair("ম্প", "ঁপ"), new NasalPair("ঙ্ক", "ঁক"), new NasalPair("ন্স", "ঁস"),
            new NasalPair("ঞ্জ", "ঁজ"), new NasalPair("ন্ঠ", "ঁঠ"), new NasalPair("ন্ড", "ঁড")
        };

        private readonly Dictionary<string, string> _autodict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "bank", "ব্যাংক" },
            { "banking", "ব্যাংকিং" },
            { "iuai", "ইউআই" },
            { "ui", "ইউআই" },
            { "ux", "ইউএক্স" },
            { "ai", "এআই" },
            { "tank", "ট্যাংক" },
            { "rank", "র‍্যাংক" },
            { "link", "লিংক" },
            { "pink", "পিংক" },
            { "sink", "সিংক" },
            { "thanks", "থ্যাংকস" },
            { "software", "সফটওয়্যার" },
            { "hardware", "হার্ডওয়্যার" },
            { "doctor", "ডাক্তার" },
            { "hospital", "হাসপাতাল" },
            { "police", "পুলিশ" },
            { "card", "কার্ড" },
            { "mobile", "মোবাইল" },
            { "phone", "ফোন" },
            { "office", "অফিস" },
            { "college", "কলেজ" },
            { "account", "অ্যাকাউন্ট" }
        };

        private BanglishDictionary()
        {
            foreach (var w in SeedWords) _words.Add(w);
            LoadDictionaryAsync();
        }

        private void LoadDictionaryAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    var asm = typeof(BanglishDictionary).Assembly;

                    // 1. Load words
                    string dictPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "words.txt");
                    if (!File.Exists(dictPath))
                    {
                        dictPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\Resources\\words.txt");
                    }

                    if (File.Exists(dictPath))
                    {
                        using (var reader = new StreamReader(dictPath, Encoding.UTF8))
                        {
                            LoadWordsFromReader(reader);
                        }
                    }
                    else
                    {
                        using (Stream gzStream = asm.GetManifestResourceStream("words.txt.gz"))
                        {
                            if (gzStream != null)
                            {
                                using (var gz = new System.IO.Compression.GZipStream(gzStream, System.IO.Compression.CompressionMode.Decompress))
                                using (var reader = new StreamReader(gz, Encoding.UTF8))
                                {
                                    LoadWordsFromReader(reader);
                                }
                            }
                            else
                            {
                                using (Stream plainStream = asm.GetManifestResourceStream("words.txt"))
                                {
                                    if (plainStream != null)
                                    {
                                        using (var reader = new StreamReader(plainStream, Encoding.UTF8))
                                        {
                                            LoadWordsFromReader(reader);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 2. Load autodict
                    string autoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autodict.txt");
                    if (!File.Exists(autoPath))
                    {
                        autoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\Resources\\autodict.txt");
                    }

                    if (File.Exists(autoPath))
                    {
                        using (var reader = new StreamReader(autoPath, Encoding.UTF8))
                        {
                            LoadAutodictFromReader(reader);
                        }
                    }
                    else
                    {
                        using (Stream autoStream = asm.GetManifestResourceStream("autodict.txt"))
                        {
                            if (autoStream != null)
                            {
                                using (var reader = new StreamReader(autoStream, Encoding.UTF8))
                                {
                                    LoadAutodictFromReader(reader);
                                }
                            }
                        }
                    }

                    lock (_lock) { _isLoaded = true; }
                }
                catch {}
            });
        }

        private void LoadWordsFromReader(TextReader reader)
        {
            string line;
            lock (_lock)
            {
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        _words.Add(trimmed);
                    }
                }
            }
        }

        private void LoadAutodictFromReader(TextReader reader)
        {
            string line;
            lock (_lock)
            {
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length == 2)
                    {
                        _autodict[parts[0].Trim().ToLowerInvariant()] = parts[1].Trim();
                    }
                }
            }
        }

        public bool Contains(string word)
        {
            lock (_lock)
            {
                return _words.Contains(word);
            }
        }

        public List<string> GetCandidates(string rawInput)
        {
            if (string.IsNullOrEmpty(rawInput)) return new List<string>();

            // Handle full stop / dot (.)
            if (rawInput == ".")
            {
                return new List<string> { "।", "." };
            }

            string preferred = null;
            lock (_lock)
            {
                _autodict.TryGetValue(rawInput.Trim().ToLowerInvariant(), out preferred);
            }

            string primary = BanglishEngine.Shared.Transliterate(rawInput);
            if (string.IsNullOrEmpty(primary))
            {
                return preferred != null ? new List<string> { preferred, rawInput } : new List<string> { rawInput };
            }

            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            // Add Candidate helper
            Action<string> addCandidate = delegate(string word)
            {
                if (string.IsNullOrEmpty(word)) return;
                string trimmed = word.Trim();
                if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                {
                    result.Add(trimmed);
                }
            };

            // 1. Preferred standard spelling is always #1 if present (e.g. bank -> ব্যাংক, iuai -> ইউআই)
            if (!string.IsNullOrEmpty(preferred))
            {
                addCandidate(preferred);
            }

            // 2. Direct engine transliteration
            addCandidate(primary);

            HashSet<string> variants = new HashSet<string>(StringComparer.Ordinal);

            // Rule A: Final ত <-> ৎ
            if (primary.EndsWith("ত")) variants.Add(primary.Substring(0, primary.Length - 1) + "ৎ");
            else if (primary.EndsWith("ৎ")) variants.Add(primary.Substring(0, primary.Length - 1) + "ত");

            // Rule B: ি <-> ী (0x09BF vs 0x09C0)
            List<string> curB = new List<string>(variants);
            curB.Add(primary);
            foreach (var w in curB)
            {
                if (w.IndexOf('\u09BF') >= 0) variants.Add(w.Replace('\u09BF', '\u09C0'));
                if (w.IndexOf('\u09C0') >= 0) variants.Add(w.Replace('\u09C0', '\u09BF'));
            }

            // Rule C: ু <-> ূ (0x09C1 vs 0x09C2)
            List<string> curC = new List<string>(variants);
            curC.Add(primary);
            foreach (var w in curC)
            {
                if (w.IndexOf('\u09C1') >= 0) variants.Add(w.Replace('\u09C1', '\u09C2'));
                if (w.IndexOf('\u09C2') >= 0) variants.Add(w.Replace('\u09C2', '\u09C1'));
            }

            // Rule D: স <-> শ <-> ষ (0x09B8, 0x09B6, 0x09B7)
            List<string> curD = new List<string>(variants);
            curD.Add(primary);
            foreach (var w in curD)
            {
                if (w.IndexOf('\u09B8') >= 0) { variants.Add(w.Replace('\u09B8', '\u09B6')); variants.Add(w.Replace('\u09B8', '\u09B7')); }
                if (w.IndexOf('\u09B6') >= 0) { variants.Add(w.Replace('\u09B6', '\u09B8')); variants.Add(w.Replace('\u09B6', '\u09B7')); }
                if (w.IndexOf('\u09B7') >= 0) { variants.Add(w.Replace('\u09B7', '\u09B6')); variants.Add(w.Replace('\u09B7', '\u09B8')); }
            }

            // Rule E: ন <-> ণ (0x09A8 vs 0x09A3)
            List<string> curE = new List<string>(variants);
            curE.Add(primary);
            foreach (var w in curE)
            {
                if (w.IndexOf('\u09A8') >= 0) variants.Add(w.Replace('\u09A8', '\u09A3'));
                if (w.IndexOf('\u09A3') >= 0) variants.Add(w.Replace('\u09A3', '\u09A8'));
            }

            // Rule F: র <-> ড় <-> ঢ় (0x09B0, 0x09DC, 0x09DD)
            List<string> curF = new List<string>(variants);
            curF.Add(primary);
            foreach (var w in curF)
            {
                if (w.IndexOf('\u09B0') >= 0) variants.Add(w.Replace('\u09B0', '\u09DC'));
                if (w.IndexOf('\u09DC') >= 0) { variants.Add(w.Replace('\u09DC', '\u09B0')); variants.Add(w.Replace('\u09DC', '\u09DD')); }
                if (w.IndexOf('\u09DD') >= 0) variants.Add(w.Replace('\u09DD', '\u09DC'));
            }

            // Rule G: জ <-> য (0x099C vs 0x09AF) e.g. হাফেয <-> হাফেজ
            List<string> curG = new List<string>(variants);
            curG.Add(primary);
            foreach (var w in curG)
            {
                if (w.IndexOf('\u099C') >= 0) variants.Add(w.Replace('\u099C', '\u09AF'));
                if (w.IndexOf('\u09AF') >= 0) variants.Add(w.Replace('\u09AF', '\u099C'));
            }

            // Rule M: ছ <-> চ (0x099B vs 0x099A)
            List<string> curM = new List<string>(variants);
            curM.Add(primary);
            foreach (var w in curM)
            {
                if (w.IndexOf('\u099B') >= 0) variants.Add(w.Replace('\u099B', '\u099A'));
                if (w.IndexOf('\u099A') >= 0) variants.Add(w.Replace('\u099A', '\u099B'));
            }

            // Rule N: ত <-> ট (0x09A4 vs 0x099F)
            List<string> curN = new List<string>(variants);
            curN.Add(primary);
            foreach (var w in curN)
            {
                if (w.IndexOf('\u09A4') >= 0) variants.Add(w.Replace('\u09A4', '\u099F'));
                if (w.IndexOf('\u099F') >= 0) variants.Add(w.Replace('\u099F', '\u09A4'));
            }

            // Rule O: o-kar ambiguity (khoj -> খোজ/খোঁজ, chok -> চোখ)
            if (rawInput.IndexOf('o') >= 0 && primary.IndexOf("ো") < 0)
            {
                string transO = BanglishEngine.Shared.Transliterate(rawInput.Replace("o", "O"));
                if (!string.IsNullOrEmpty(transO)) variants.Add(transO);
            }

            // Rule P: Y, Ja-fola (্য), Antostho-A (য়), Vowel (আই/ই) & W/War (ওয়্যার) variants
            // e.g. "iuai" -> "ইউয়াই" -> "ইউআই"
            // "softowar" / "software" -> "সফটওয়্যার"
            List<string> curP = new List<string>(variants);
            curP.Add(primary);
            foreach (var w in curP)
            {
                // 1. য় <-> ্য (y used as ya-fola vs antostho-a)
                // e.g. ক্য <-> কয়, ব্য <-> বয়, ন্য <-> নয়
                if (w.IndexOf("্য") >= 0) variants.Add(w.Replace("্য", "য়"));
                if (w.IndexOf("য়") >= 0) variants.Add(w.Replace("য়", "্য"));

                // 2. য় <-> আ / আই / ই
                // e.g. "ইউয়াই" -> "ইউআই", "দেয়া" -> "দয়া" / "দেওয়া"
                if (w.IndexOf("য়াই") >= 0) variants.Add(w.Replace("য়াই", "আই"));
                if (w.IndexOf("য়ি") >= 0) variants.Add(w.Replace("য়ি", "ই"));
                if (w.IndexOf("ইয়া") >= 0) variants.Add(w.Replace("ইয়া", "িয়া"));
                if (w.IndexOf("য়া") >= 0) variants.Add(w.Replace("য়া", "আ"));

                // 3. তও <-> ত্ব / ত্ব <-> তও (e.g. সফতওার -> সফটওয়্যার, কতওয়াল -> কোতোয়াল)
                if (w.IndexOf("তও") >= 0) variants.Add(w.Replace("তও", "ত্ব"));
                if (w.IndexOf("ত্ব") >= 0) variants.Add(w.Replace("ত্ব", "তও"));

                // 4. ওার / ও্যার / ত্বার / তওার <-> ওয়্যার (e.g. সফতওার / সফত্বার / সফটও্যার -> সফটওয়্যার)
                if (w.IndexOf("তওার") >= 0) variants.Add(w.Replace("তওার", "টওয়্যার"));
                if (w.IndexOf("ত্বার") >= 0) variants.Add(w.Replace("ত্বার", "টওয়্যার"));
                if (w.IndexOf("ত্বারে") >= 0) variants.Add(w.Replace("ত্বারে", "টওয়্যার"));
                if (w.IndexOf("ওার") >= 0) variants.Add(w.Replace("ওার", "ওয়্যার"));
                if (w.IndexOf("ও্যার") >= 0) variants.Add(w.Replace("ও্যার", "ওয়্যার"));
                if (w.IndexOf("অ্যার") >= 0) variants.Add(w.Replace("অ্যার", "ওয়্যার"));
                if (w.IndexOf("ওয়ার") >= 0) variants.Add(w.Replace("ওয়ার", "ওয়্যার"));

                // 5. ত <-> ট before ওয়্যার / ্য (e.g. সফতওয়্যার -> সফটওয়্যার)
                if (w.IndexOf("ফত") >= 0) variants.Add(w.Replace("ফত", "ফট"));
            }

            // Also check phonetic transliteration variations of rawInput for 'y' and 'w'
            if (rawInput.IndexOf('y') >= 0)
            {
                // 'y' as 'Y' (forces য়)
                string transY = BanglishEngine.Shared.Transliterate(rawInput.Replace("y", "Y"));
                if (!string.IsNullOrEmpty(transY)) variants.Add(transY);
                // 'y' as 'Z' (forces ্য)
                string transZ = BanglishEngine.Shared.Transliterate(rawInput.Replace("y", "Z"));
                if (!string.IsNullOrEmpty(transZ)) variants.Add(transZ);
            }
            if (rawInput.IndexOf("war") >= 0 || rawInput.IndexOf("wer") >= 0 || rawInput.IndexOf("ware") >= 0)
            {
                // war -> wZar -> ওয়্যার
                string modInput = rawInput.Replace("software", "softwZar").Replace("softowar", "softwZar").Replace("softwer", "softwZar").Replace("ware", "wZar");
                string transW = BanglishEngine.Shared.Transliterate(modInput);
                if (!string.IsNullOrEmpty(transW))
                {
                    variants.Add(transW);
                    if (transW.IndexOf("ফত") >= 0) variants.Add(transW.Replace("ফত", "ফট"));
                }
            }

            // Rule H: ং <-> ঙ
            List<string> curH = new List<string>(variants);
            curH.Add(primary);
            foreach (var w in curH)
            {
                if (w.IndexOf("ং") >= 0) variants.Add(w.Replace("ং", "ঙ"));
                if (w.IndexOf("ঙ") >= 0) variants.Add(w.Replace("ঙ", "ং"));
            }

            // Rule I: Juktoborno variants
            List<string> curI = new List<string>(variants);
            curI.Add(primary);
            foreach (var w in curI)
            {
                if (w.IndexOf("ন্ত") >= 0) variants.Add(w.Replace("ন্ত", "ণ্ট"));
                if (w.IndexOf("ণ্ট") >= 0) variants.Add(w.Replace("ণ্ট", "ন্ত"));
                if (w.IndexOf("ন্দ") >= 0) variants.Add(w.Replace("ন্দ", "ণ্ড"));
                if (w.IndexOf("ণ্ড") >= 0) variants.Add(w.Replace("ণ্ড", "ন্দ"));
                if (w.IndexOf("স্ত") >= 0) variants.Add(w.Replace("স্ত", "ষ্ট"));
                if (w.IndexOf("ষ্ট") >= 0) variants.Add(w.Replace("ষ্ট", "স্ত"));
                if (w.IndexOf("স্থ") >= 0) variants.Add(w.Replace("স্থ", "ষ্ঠ"));
                if (w.IndexOf("ষ্ঠ") >= 0) variants.Add(w.Replace("ষ্ঠ", "স্থ"));
                if (w.IndexOf("স্প") >= 0) variants.Add(w.Replace("স্প", "ষ্প"));
                if (w.IndexOf("ষ্প") >= 0) variants.Add(w.Replace("ষ্প", "স্প"));
                if (w.IndexOf("ঙ্ক") >= 0) variants.Add(w.Replace("ঙ্ক", "ংক"));
                if (w.IndexOf("ংক") >= 0) variants.Add(w.Replace("ংক", "ঙ্ক"));
                if (w.IndexOf("বাঙ্ক") >= 0) { variants.Add(w.Replace("বাঙ্ক", "ব্যাংক")); variants.Add(w.Replace("বাঙ্ক", "ব্যাঙ্ক")); }
                if (w.IndexOf("টাঙ্ক") >= 0) { variants.Add(w.Replace("টাঙ্ক", "ট্যাংক")); variants.Add(w.Replace("টাঙ্ক", "ট্যাঙ্ক")); }
                if (w.IndexOf("রাঙ্ক") >= 0) variants.Add(w.Replace("রাঙ্ক", "র‍্যাংক"));
                if (w.IndexOf("লাঙ্ক") >= 0) variants.Add(w.Replace("লাঙ্ক", "লিংক"));
                if (w.IndexOf("জ্ঞ") >= 0) variants.Add(w.Replace("জ্ঞ", "গ্য"));
                if (w.IndexOf("ক্ষ") >= 0) variants.Add(w.Replace("ক্ষ", "খ"));
            }

            // Rule Q: English loanword short 'a' variations
            List<string> curQ = new List<string>(variants);
            curQ.Add(primary);
            foreach (var w in curQ)
            {
                if (w.StartsWith("বা") && !w.StartsWith("বাংলাদেশ") && !w.StartsWith("বাংলা")) variants.Add("ব্যা" + w.Substring(2));
                if (w.StartsWith("কা") && !w.StartsWith("কাজ") && !w.StartsWith("কারণ")) variants.Add("ক্যা" + w.Substring(2));
                if (w.StartsWith("টা") && !w.StartsWith("টাকা")) variants.Add("ট্যা" + w.Substring(2));
                if (w.StartsWith("গা") && !w.StartsWith("গান")) variants.Add("গ্যা" + w.Substring(2));
                if (w.StartsWith("ফা")) variants.Add("ফ্যা" + w.Substring(2));
            }

            // Rule J: Reph & Ro-fola
            List<string> curJ = new List<string>(variants);
            curJ.Add(primary);
            foreach (var w in curJ)
            {
                foreach (var c in BengaliConsonants)
                {
                    string target = "র" + c;
                    if (w.IndexOf(target) >= 0) variants.Add(w.Replace(target, "র্" + c));
                }
                foreach (var c in BengaliConsonants)
                {
                    if (c == "র" || c == "ড়" || c == "ঢ়" || c == "ৎ") continue;
                    string target = c + "র";
                    if (w.IndexOf(target) >= 0) variants.Add(w.Replace(target, c + "্র"));
                }
            }

            // Rule K: Double R reph
            if (rawInput.IndexOf('r') >= 0 && rawInput.IndexOf("rr") < 0)
            {
                string transRR = BanglishEngine.Shared.Transliterate(rawInput.Replace("r", "rr"));
                if (!string.IsNullOrEmpty(transRR)) variants.Add(transRR);
            }

            // Rule L: Chandrabindu (ঁ) e.g. chad -> চাঁদ, has -> হাঁস, badha -> বাঁধা
            List<string> curL = new List<string>(variants);
            curL.Add(primary);
            foreach (var w in curL)
            {
                foreach (var pair in NasalPairs)
                {
                    if (w.IndexOf(pair.From) >= 0) variants.Add(w.Replace(pair.From, pair.To));
                }

                for (int i = 0; i < w.Length; i++)
                {
                    char ch = w[i];
                    if ("ািীুূৃেৈোৌ".IndexOf(ch) >= 0 || "অআইঈউঊঋএঐওঔ".IndexOf(ch) >= 0)
                    {
                        if (i + 1 < w.Length && w[i + 1] == 'ঁ') continue;
                        variants.Add(w.Insert(i + 1, "ঁ"));
                    }
                }
            }

            // Prioritize dictionary confirmed variants:
            List<string> confirmedList = new List<string>();
            List<string> unconfirmedList = new List<string>();

            lock (_lock)
            {
                foreach (var v in variants)
                {
                    if (string.IsNullOrEmpty(v)) continue;
                    if (_words.Contains(v))
                    {
                        confirmedList.Add(v);
                    }
                    else
                    {
                        unconfirmedList.Add(v);
                    }
                }
            }

            // Confirmed dictionary words come first (e.g. ইউআই, সফটওয়্যার, হাফেজ)
            foreach (var v in confirmedList)
            {
                if (result.Count >= 7) break;
                addCandidate(v);
            }

            // Other unconfirmed phonetic variants (sort by length and distance from primary)
            foreach (var v in unconfirmedList)
            {
                if (result.Count >= 7) break;
                // Avoid too many random chandrabindu variants if unconfirmed
                if (v.IndexOf('ঁ') >= 0 && !confirmedList.Contains(v)) continue;
                addCandidate(v);
            }

            // If still room, allow remaining variants
            foreach (var v in unconfirmedList)
            {
                if (result.Count >= 7) break;
                addCandidate(v);
            }

            // Always append raw Latin text as the last candidate (Mac reference style)
            addCandidate(rawInput);

            return result;
        }
    }
}
