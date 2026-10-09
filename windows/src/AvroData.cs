using System;
using System.Collections.Generic;

namespace Banglish.Core
{
    public class AvroMatch
    {
        public string Type { get; private set; }     // "s" (suffix) or "p" (prefix)
        public string Scope { get; private set; }    // "p" (punctuation/bound), "v" (vowel), "c" (consonant), "e" (exact)
        public bool Negative { get; private set; }
        public string Value { get; private set; }

        public AvroMatch(string type, string scope, bool negative, string value = "")
        {
            Type = type;
            Scope = scope;
            Negative = negative;
            Value = value;
        }
    }

    public class AvroRule
    {
        public List<AvroMatch> Matches { get; private set; }
        public string Replace { get; private set; }

        public AvroRule(List<AvroMatch> matches, string replace)
        {
            Matches = matches;
            Replace = replace;
        }
    }

    public class AvroPattern
    {
        public string Find { get; private set; }
        public string Replace { get; private set; }
        public List<AvroRule> Rules { get; private set; }

        public AvroPattern(string find, string replace, List<AvroRule> rules = null)
        {
            Find = find;
            Replace = replace;
            Rules = rules;
        }
    }

    public static class AvroData
    {
        public const string Vowels = "aeiou";
        public const string Consonants = "bcdfghjklmnpqrstvwxyz";
        public const string CaseSensitive = "oiudgjnrstyz";

        public static readonly List<AvroPattern> Patterns = new List<AvroPattern>
        {
            new AvroPattern("bhl", "ভ্ল"),
            new AvroPattern("psh", "পশ"),
            new AvroPattern("bdh", "ব্ধ"),
            new AvroPattern("bj", "ব্জ"),
            new AvroPattern("bd", "ব্দ"),
            new AvroPattern("bb", "ব্ব"),
            new AvroPattern("bl", "ব্ল"),
            new AvroPattern("bh", "ভ"),
            new AvroPattern("vl", "ভ্ল"),
            new AvroPattern("b", "ব"),
            new AvroPattern("v", "ভ"),
            new AvroPattern("cNG", "চ্ঞ"),
            new AvroPattern("cch", "চ্ছ"),
            new AvroPattern("cc", "চ্চ"),
            new AvroPattern("ch", "ছ"),
            new AvroPattern("c", "চ"),
            new AvroPattern("dhn", "ধ্ন"),
            new AvroPattern("dhm", "ধ্ম"),
            new AvroPattern("dgh", "দ্ঘ"),
            new AvroPattern("ddh", "দ্ধ"),
            new AvroPattern("dbh", "দ্ভ"),
            new AvroPattern("dv", "দ্ভ"),
            new AvroPattern("dm", "দ্ম"),
            new AvroPattern("DD", "ড্ড"),
            new AvroPattern("Dh", "ঢ"),
            new AvroPattern("dh", "ধ"),
            new AvroPattern("dg", "দ্গ"),
            new AvroPattern("dd", "দ্দ"),
            new AvroPattern("D", "ড"),
            new AvroPattern("d", "দ"),
            new AvroPattern("...", "..."),
            new AvroPattern(".`", "."),
            new AvroPattern("..", "।।"),
            new AvroPattern(".", "।"),
            new AvroPattern("ghn", "ঘ্ন"),
            new AvroPattern("Ghn", "ঘ্ন"),
            new AvroPattern("gdh", "গ্ধ"),
            new AvroPattern("Gdh", "গ্ধ"),
            new AvroPattern("gN", "গ্ণ"),
            new AvroPattern("GN", "গ্ণ"),
            new AvroPattern("gn", "গ্ন"),
            new AvroPattern("Gn", "গ্ন"),
            new AvroPattern("gm", "গ্ম"),
            new AvroPattern("Gm", "গ্ম"),
            new AvroPattern("gl", "গ্ল"),
            new AvroPattern("Gl", "গ্ল"),
            new AvroPattern("gg", "জ্ঞ"),
            new AvroPattern("GG", "জ্ঞ"),
            new AvroPattern("Gg", "জ্ঞ"),
            new AvroPattern("gG", "জ্ঞ"),
            new AvroPattern("gh", "ঘ"),
            new AvroPattern("Gh", "ঘ"),
            new AvroPattern("g", "গ"),
            new AvroPattern("G", "গ"),
            new AvroPattern("hN", "হ্ণ"),
            new AvroPattern("hn", "হ্ন"),
            new AvroPattern("hm", "হ্ম"),
            new AvroPattern("hl", "হ্ল"),
            new AvroPattern("h", "হ"),
            new AvroPattern("jjh", "জ্ঝ"),
            new AvroPattern("jNG", "জ্ঞ"),
            new AvroPattern("jh", "ঝ"),
            new AvroPattern("jj", "জ্জ"),
            new AvroPattern("j", "জ"),
            new AvroPattern("J", "জ"),
            new AvroPattern("kkhN", "ক্ষ্ণ"),
            new AvroPattern("kShN", "ক্ষ্ণ"),
            new AvroPattern("kkhm", "ক্ষ্ম"),
            new AvroPattern("kShm", "ক্ষ্ম"),
            new AvroPattern("kxN", "ক্ষ্ণ"),
            new AvroPattern("kxm", "ক্ষ্ম"),
            new AvroPattern("kkh", "ক্ষ"),
            new AvroPattern("kSh", "ক্ষ"),
            new AvroPattern("ksh", "কশ"),
            new AvroPattern("kx", "ক্ষ"),
            new AvroPattern("kk", "ক্ক"),
            new AvroPattern("kT", "ক্ট"),
            new AvroPattern("kt", "ক্ত"),
            new AvroPattern("kl", "ক্ল"),
            new AvroPattern("ks", "ক্স"),
            new AvroPattern("kh", "খ"),
            new AvroPattern("k", "ক"),
            new AvroPattern("lbh", "ল্ভ"),
            new AvroPattern("ldh", "ল্ধ"),
            new AvroPattern("lkh", "লখ"),
            new AvroPattern("lgh", "লঘ"),
            new AvroPattern("lph", "লফ"),
            new AvroPattern("lk", "ল্ক"),
            new AvroPattern("lg", "ল্গ"),
            new AvroPattern("lT", "ল্ট"),
            new AvroPattern("lD", "ল্ড"),
            new AvroPattern("lp", "ল্প"),
            new AvroPattern("lv", "ল্ভ"),
            new AvroPattern("lm", "ল্ম"),
            new AvroPattern("ll", "ল্ল"),
            new AvroPattern("lb", "ল্ব"),
            new AvroPattern("l", "ল"),
            new AvroPattern("mth", "ম্থ"),
            new AvroPattern("mph", "ম্ফ"),
            new AvroPattern("mbh", "ম্ভ"),
            new AvroPattern("mpl", "মপ্ল"),
            new AvroPattern("mn", "ম্ন"),
            new AvroPattern("mp", "ম্প"),
            new AvroPattern("mv", "ম্ভ"),
            new AvroPattern("mm", "ম্ম"),
            new AvroPattern("ml", "ম্ল"),
            new AvroPattern("mb", "ম্ব"),
            new AvroPattern("mf", "ম্ফ"),
            new AvroPattern("m", "ম"),
            new AvroPattern("0", "০"),
            new AvroPattern("1", "১"),
            new AvroPattern("2", "২"),
            new AvroPattern("3", "৩"),
            new AvroPattern("4", "৪"),
            new AvroPattern("5", "৫"),
            new AvroPattern("6", "৬"),
            new AvroPattern("7", "৭"),
            new AvroPattern("8", "৮"),
            new AvroPattern("9", "৯"),
            new AvroPattern("NgkSh", "ঙ্ক্ষ"),
            new AvroPattern("Ngkkh", "ঙ্ক্ষ"),
            new AvroPattern("NGch", "ঞ্ছ"),
            new AvroPattern("Nggh", "ঙ্ঘ"),
            new AvroPattern("Ngkh", "ঙ্খ"),
            new AvroPattern("NGjh", "ঞ্ঝ"),
            new AvroPattern("ngOU", "ঙ্গৌ"),
            new AvroPattern("ngOI", "ঙ্গৈ"),
            new AvroPattern("Ngkx", "ঙ্ক্ষ"),
            new AvroPattern("NGc", "ঞ্চ"),
            new AvroPattern("nch", "ঞ্ছ"),
            new AvroPattern("njh", "ঞ্ঝ"),
            new AvroPattern("ngh", "ঙ্ঘ"),
            new AvroPattern("Ngk", "ঙ্ক"),
            new AvroPattern("Ngx", "ঙ্ষ"),
            new AvroPattern("Ngg", "ঙ্গ"),
            new AvroPattern("Ngm", "ঙ্ম"),
            new AvroPattern("NGj", "ঞ্জ"),
            new AvroPattern("ndh", "ন্ধ"),
            new AvroPattern("nTh", "ন্ঠ"),
            new AvroPattern("NTh", "ণ্ঠ"),
            new AvroPattern("nth", "ন্থ"),
            new AvroPattern("nkh", "ঙ্খ"),
            new AvroPattern("ngo", "ঙ্গ"),
            new AvroPattern("nga", "ঙ্গা"),
            new AvroPattern("ngi", "ঙ্গি"),
            new AvroPattern("ngI", "ঙ্গী"),
            new AvroPattern("ngu", "ঙ্গু"),
            new AvroPattern("ngU", "ঙ্গূ"),
            new AvroPattern("nge", "ঙ্গে"),
            new AvroPattern("ngO", "ঙ্গো"),
            new AvroPattern("NDh", "ণ্ঢ"),
            new AvroPattern("nsh", "নশ"),
            new AvroPattern("Ngr", "ঙর"),
            new AvroPattern("NGr", "ঞর"),
            new AvroPattern("ngr", "ংর"),
            new AvroPattern("nj", "ঞ্জ"),
            new AvroPattern("Ng", "ঙ"),
            new AvroPattern("NG", "ঞ"),
            new AvroPattern("nk", "ঙ্ক"),
            new AvroPattern("ng", "ং"),
            new AvroPattern("nn", "ন্ন"),
            new AvroPattern("NN", "ণ্ণ"),
            new AvroPattern("Nn", "ণ্ন"),
            new AvroPattern("nm", "ন্ম"),
            new AvroPattern("Nm", "ণ্ম"),
            new AvroPattern("nd", "ন্দ"),
            new AvroPattern("nT", "ন্ট"),
            new AvroPattern("NT", "ণ্ট"),
            new AvroPattern("nD", "ন্ড"),
            new AvroPattern("ND", "ণ্ড"),
            new AvroPattern("nt", "ন্ত"),
            new AvroPattern("ns", "ন্স"),
            new AvroPattern("nc", "ঞ্চ"),
            new AvroPattern("n", "ন"),
            new AvroPattern("N", "ণ"),
            new AvroPattern("OI`", "ৈ"),
            new AvroPattern("OU`", "ৌ"),
            new AvroPattern("O`", "ো"),
            new AvroPattern("OI", "ৈ", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> { new AvroMatch("p", "c", true, "") }, "ঐ"),
                new AvroRule(new List<AvroMatch> { new AvroMatch("p", "p", false, "") }, "ঐ")
            }),
            new AvroPattern("OU", "ৌ", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> { new AvroMatch("p", "c", true, "") }, "ঔ"),
                new AvroRule(new List<AvroMatch> { new AvroMatch("p", "p", false, "") }, "ঔ")
            }),
            new AvroPattern("O", "ো", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> { new AvroMatch("p", "c", true, "") }, "ও"),
                new AvroRule(new List<AvroMatch> { new AvroMatch("p", "p", false, "") }, "ও")
            }),
            new AvroPattern("phl", "ফ্ল"),
            new AvroPattern("pT", "প্ট"),
            new AvroPattern("pt", "প্ত"),
            new AvroPattern("pn", "প্ন"),
            new AvroPattern("pp", "প্প"),
            new AvroPattern("pl", "প্ল"),
            new AvroPattern("ps", "প্স"),
            new AvroPattern("ph", "ফ"),
            new AvroPattern("fl", "ফ্ল"),
            new AvroPattern("f", "ফ"),
            new AvroPattern("p", "প"),
            new AvroPattern("rri`", "ৃ"),
            new AvroPattern("rri", "ৃ", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> { new AvroMatch("p", "c", true, "") }, "ঋ"),
                new AvroRule(new List<AvroMatch> { new AvroMatch("p", "p", false, "") }, "ঋ")
            }),
            new AvroPattern("rrZ", "রর‍্য"),
            new AvroPattern("rry", "রর‍্য"),
            new AvroPattern("rZ", "র‍্য", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", false, ""),
                    new AvroMatch("p", "e", true, "r"),
                    new AvroMatch("p", "e", true, "y"),
                    new AvroMatch("p", "e", true, "w"),
                    new AvroMatch("p", "e", true, "x")
                }, "্র্য")
            }),
            new AvroPattern("ry", "র‍্য", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", false, ""),
                    new AvroMatch("p", "e", true, "r"),
                    new AvroMatch("p", "e", true, "y"),
                    new AvroMatch("p", "e", true, "w"),
                    new AvroMatch("p", "e", true, "x")
                }, "্র্য")
            }),
            new AvroPattern("rr", "রর", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", true, ""),
                    new AvroMatch("s", "v", true, ""),
                    new AvroMatch("s", "e", true, "r"),
                    new AvroMatch("s", "p", true, "")
                }, "র্"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", false, ""),
                    new AvroMatch("p", "e", true, "r")
                }, "্রর")
            }),
            new AvroPattern("Rg", "ড়্গ"),
            new AvroPattern("Rh", "ঢ়"),
            new AvroPattern("R", "ড়"),
            new AvroPattern("r", "র", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", false, ""),
                    new AvroMatch("p", "e", true, "r"),
                    new AvroMatch("p", "e", true, "y"),
                    new AvroMatch("p", "e", true, "w"),
                    new AvroMatch("p", "e", true, "x"),
                    new AvroMatch("p", "e", true, "Z")
                }, "্র")
            }),
            new AvroPattern("shch", "শ্ছ"),
            new AvroPattern("ShTh", "ষ্ঠ"),
            new AvroPattern("Shph", "ষ্ফ"),
            new AvroPattern("Sch", "শ্ছ"),
            new AvroPattern("skl", "স্ক্ল"),
            new AvroPattern("skh", "স্খ"),
            new AvroPattern("sth", "স্থ"),
            new AvroPattern("sph", "স্ফ"),
            new AvroPattern("shc", "শ্চ"),
            new AvroPattern("sht", "শ্চ"),
            new AvroPattern("shn", "শ্ন"),
            new AvroPattern("shm", "শ্ম"),
            new AvroPattern("shl", "শ্ল"),
            new AvroPattern("Shk", "ষ্ক"),
            new AvroPattern("ShT", "ষ্ট"),
            new AvroPattern("ShN", "ষ্ণ"),
            new AvroPattern("Shp", "ষ্প"),
            new AvroPattern("Shf", "ষ্ফ"),
            new AvroPattern("Shm", "ষ্ম"),
            new AvroPattern("spl", "স্প্ল"),
            new AvroPattern("sk", "স্ক"),
            new AvroPattern("Sc", "শ্চ"),
            new AvroPattern("sT", "স্ট"),
            new AvroPattern("st", "স্ত"),
            new AvroPattern("sn", "স্ন"),
            new AvroPattern("sp", "স্প"),
            new AvroPattern("sf", "স্ফ"),
            new AvroPattern("sm", "স্ম"),
            new AvroPattern("sl", "স্ল"),
            new AvroPattern("sh", "শ"),
            new AvroPattern("St", "শ্ত"),
            new AvroPattern("Sn", "শ্ন"),
            new AvroPattern("Sm", "শ্ম"),
            new AvroPattern("Sl", "শ্ল"),
            new AvroPattern("Sh", "ষ"),
            new AvroPattern("s", "স"),
            new AvroPattern("S", "শ"),
            new AvroPattern("oo`", "ু"),
            new AvroPattern("oo", "ু", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", true, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "উ"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "উ")
            }),
            new AvroPattern("o`", ""),
            new AvroPattern("oZ", "অ্য"),
            new AvroPattern("o", "", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "v", false, ""),
                    new AvroMatch("p", "e", true, "o")
                }, "ও"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "v", false, ""),
                    new AvroMatch("p", "e", false, "o")
                }, "অ"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, "")
                }, "অ")
            }),
            new AvroPattern("tth", "ত্থ"),
            new AvroPattern("t``", "ৎ"),
            new AvroPattern("TT", "ট্ট"),
            new AvroPattern("Tm", "ট্ম"),
            new AvroPattern("Th", "ঠ"),
            new AvroPattern("tn", "ত্ন"),
            new AvroPattern("tm", "ত্ম"),
            new AvroPattern("th", "থ"),
            new AvroPattern("tt", "ত্ত"),
            new AvroPattern("T", "ট"),
            new AvroPattern("t", "ত"),
            new AvroPattern("aZ", "অ্যা"),
            new AvroPattern("AZ", "অ্যা"),
            new AvroPattern("a`", "া"),
            new AvroPattern("A`", "া"),
            new AvroPattern("a", "া", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "আ"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", true, ""),
                    new AvroMatch("p", "e", true, "a"),
                    new AvroMatch("s", "e", true, "`")
                }, "য়া"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "e", false, "a"),
                    new AvroMatch("s", "e", true, "`")
                }, "আ")
            }),
            new AvroPattern("i`", "ি"),
            new AvroPattern("i", "ি", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", true, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "ই"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "ই")
            }),
            new AvroPattern("I`", "ী"),
            new AvroPattern("I", "ী", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", true, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "ঈ"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "ঈ")
            }),
            new AvroPattern("u`", "ু"),
            new AvroPattern("u", "ু", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", true, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "উ"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "উ")
            }),
            new AvroPattern("U`", "ূ"),
            new AvroPattern("U", "ূ", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", true, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "ঊ"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "ঊ")
            }),
            new AvroPattern("ee`", "ী"),
            new AvroPattern("ee", "ী", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", true, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "ঈ"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "ঈ")
            }),
            new AvroPattern("e`", "ে"),
            new AvroPattern("e", "ে", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", true, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "এ"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, ""),
                    new AvroMatch("s", "e", true, "`")
                }, "এ")
            }),
            new AvroPattern("z", "য"),
            new AvroPattern("Z", "্য"),
            new AvroPattern("y", "্য", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", true, ""),
                    new AvroMatch("p", "p", true, "")
                }, "য়"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, "")
                }, "ইয়")
            }),
            new AvroPattern("Y", "য়"),
            new AvroPattern("q", "ক"),
            new AvroPattern("w", "ও", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, ""),
                    new AvroMatch("s", "v", false, "")
                }, "ওয়"),
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "c", false, "")
                }, "্ব")
            }),
            new AvroPattern("x", "ক্স", new List<AvroRule> {
                new AvroRule(new List<AvroMatch> {
                    new AvroMatch("p", "p", false, "")
                }, "এক্স")
            }),
            new AvroPattern(":`", ":"),
            new AvroPattern(":", "ঃ"),
            new AvroPattern("^`", "^"),
            new AvroPattern("^", "ঁ"),
            new AvroPattern(",,", "্‌"),
            new AvroPattern(",", ","),
            new AvroPattern("$", "৳"),
            new AvroPattern("`", "")
        };
    }
}
