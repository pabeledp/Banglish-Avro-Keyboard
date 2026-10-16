using System;
using System.Collections.Generic;

namespace Banglish.Core
{
    public class BanglishMatch
    {
        public string Type { get; private set; }     // "s" (suffix) or "p" (prefix)
        public string Scope { get; private set; }    // "p" (punctuation/bound), "v" (vowel), "c" (consonant), "e" (exact)
        public bool Negative { get; private set; }
        public string Value { get; private set; }

        public BanglishMatch(string type, string scope, bool negative, string value = "")
        {
            Type = type;
            Scope = scope;
            Negative = negative;
            Value = value;
        }
    }

    public class BanglishRule
    {
        public List<BanglishMatch> Matches { get; private set; }
        public string Replace { get; private set; }

        public BanglishRule(List<BanglishMatch> matches, string replace)
        {
            Matches = matches;
            Replace = replace;
        }
    }

    public class BanglishPattern
    {
        public string Find { get; private set; }
        public string Replace { get; private set; }
        public List<BanglishRule> Rules { get; private set; }

        public BanglishPattern(string find, string replace, List<BanglishRule> rules = null)
        {
            Find = find;
            Replace = replace;
            Rules = rules;
        }
    }

    public static class BanglishData
    {
        public const string Vowels = "aeiou";
        public const string Consonants = "bcdfghjklmnpqrstvwxyz";
        public const string CaseSensitive = "oiudgjnrstyz";

        public static readonly List<BanglishPattern> Patterns = new List<BanglishPattern>
        {
            new BanglishPattern("bhl", "ভ্ল"),
            new BanglishPattern("psh", "পশ"),
            new BanglishPattern("bdh", "ব্ধ"),
            new BanglishPattern("bj", "ব্জ"),
            new BanglishPattern("bd", "ব্দ"),
            new BanglishPattern("bb", "ব্ব"),
            new BanglishPattern("bl", "ব্ল"),
            new BanglishPattern("bh", "ভ"),
            new BanglishPattern("vl", "ভ্ল"),
            new BanglishPattern("b", "ব"),
            new BanglishPattern("v", "ভ"),
            new BanglishPattern("cNG", "চ্ঞ"),
            new BanglishPattern("cch", "চ্ছ"),
            new BanglishPattern("cc", "চ্চ"),
            new BanglishPattern("ch", "ছ"),
            new BanglishPattern("c", "চ"),
            new BanglishPattern("dhn", "ধ্ন"),
            new BanglishPattern("dhm", "ধ্ম"),
            new BanglishPattern("dgh", "দ্ঘ"),
            new BanglishPattern("ddh", "দ্ধ"),
            new BanglishPattern("dbh", "দ্ভ"),
            new BanglishPattern("dv", "দ্ভ"),
            new BanglishPattern("dm", "দ্ম"),
            new BanglishPattern("DD", "ড্ড"),
            new BanglishPattern("Dh", "ঢ"),
            new BanglishPattern("dh", "ধ"),
            new BanglishPattern("dg", "দ্গ"),
            new BanglishPattern("dd", "দ্দ"),
            new BanglishPattern("D", "ড"),
            new BanglishPattern("d", "দ"),
            new BanglishPattern("...", "..."),
            new BanglishPattern(".`", "."),
            new BanglishPattern("..", "।।"),
            new BanglishPattern(".", "।"),
            new BanglishPattern("ghn", "ঘ্ন"),
            new BanglishPattern("Ghn", "ঘ্ন"),
            new BanglishPattern("gdh", "গ্ধ"),
            new BanglishPattern("Gdh", "গ্ধ"),
            new BanglishPattern("gN", "গ্ণ"),
            new BanglishPattern("GN", "গ্ণ"),
            new BanglishPattern("gn", "গ্ন"),
            new BanglishPattern("Gn", "গ্ন"),
            new BanglishPattern("gm", "গ্ম"),
            new BanglishPattern("Gm", "গ্ম"),
            new BanglishPattern("gl", "গ্ল"),
            new BanglishPattern("Gl", "গ্ল"),
            new BanglishPattern("gg", "জ্ঞ"),
            new BanglishPattern("GG", "জ্ঞ"),
            new BanglishPattern("Gg", "জ্ঞ"),
            new BanglishPattern("gG", "জ্ঞ"),
            new BanglishPattern("gh", "ঘ"),
            new BanglishPattern("Gh", "ঘ"),
            new BanglishPattern("g", "গ"),
            new BanglishPattern("G", "গ"),
            new BanglishPattern("hN", "হ্ণ"),
            new BanglishPattern("hn", "হ্ন"),
            new BanglishPattern("hm", "হ্ম"),
            new BanglishPattern("hl", "হ্ল"),
            new BanglishPattern("h", "হ"),
            new BanglishPattern("jjh", "জ্ঝ"),
            new BanglishPattern("jNG", "জ্ঞ"),
            new BanglishPattern("jh", "ঝ"),
            new BanglishPattern("jj", "জ্জ"),
            new BanglishPattern("j", "জ"),
            new BanglishPattern("J", "জ"),
            new BanglishPattern("kkhN", "ক্ষ্ণ"),
            new BanglishPattern("kShN", "ক্ষ্ণ"),
            new BanglishPattern("kkhm", "ক্ষ্ম"),
            new BanglishPattern("kShm", "ক্ষ্ম"),
            new BanglishPattern("kxN", "ক্ষ্ণ"),
            new BanglishPattern("kxm", "ক্ষ্ম"),
            new BanglishPattern("kkh", "ক্ষ"),
            new BanglishPattern("kSh", "ক্ষ"),
            new BanglishPattern("ksh", "কশ"),
            new BanglishPattern("kx", "ক্ষ"),
            new BanglishPattern("kk", "ক্ক"),
            new BanglishPattern("kT", "ক্ট"),
            new BanglishPattern("kt", "ক্ত"),
            new BanglishPattern("km", "ক্ম"),
            new BanglishPattern("kl", "ক্ল"),
            new BanglishPattern("ks", "ক্স"),
            new BanglishPattern("kh", "খ"),
            new BanglishPattern("k", "ক"),
            new BanglishPattern("lbh", "ল্ভ"),
            new BanglishPattern("ldh", "ল্ধ"),
            new BanglishPattern("lkh", "লখ"),
            new BanglishPattern("lgh", "লঘ"),
            new BanglishPattern("lph", "লফ"),
            new BanglishPattern("lk", "ল্ক"),
            new BanglishPattern("lg", "ল্গ"),
            new BanglishPattern("lT", "ল্ট"),
            new BanglishPattern("lD", "ল্ড"),
            new BanglishPattern("lp", "ল্প"),
            new BanglishPattern("lv", "ল্ভ"),
            new BanglishPattern("lm", "ল্ম"),
            new BanglishPattern("ll", "ল্ল"),
            new BanglishPattern("lb", "ল্ব"),
            new BanglishPattern("l", "ল"),
            new BanglishPattern("mth", "ম্থ"),
            new BanglishPattern("mph", "ম্ফ"),
            new BanglishPattern("mbh", "ম্ভ"),
            new BanglishPattern("mpl", "মপ্ল"),
            new BanglishPattern("mn", "ম্ন"),
            new BanglishPattern("mp", "ম্প"),
            new BanglishPattern("mv", "ম্ভ"),
            new BanglishPattern("mm", "ম্ম"),
            new BanglishPattern("ml", "ম্ল"),
            new BanglishPattern("mb", "ম্ব"),
            new BanglishPattern("mf", "ম্ফ"),
            new BanglishPattern("m", "ম"),
            new BanglishPattern("0", "০"),
            new BanglishPattern("1", "১"),
            new BanglishPattern("2", "২"),
            new BanglishPattern("3", "৩"),
            new BanglishPattern("4", "৪"),
            new BanglishPattern("5", "৫"),
            new BanglishPattern("6", "৬"),
            new BanglishPattern("7", "৭"),
            new BanglishPattern("8", "৮"),
            new BanglishPattern("9", "৯"),
            new BanglishPattern("NgkSh", "ঙ্ক্ষ"),
            new BanglishPattern("Ngkkh", "ঙ্ক্ষ"),
            new BanglishPattern("NGch", "ঞ্ছ"),
            new BanglishPattern("Nggh", "ঙ্ঘ"),
            new BanglishPattern("Ngkh", "ঙ্খ"),
            new BanglishPattern("NGjh", "ঞ্ঝ"),
            new BanglishPattern("ngOU", "ঙ্গৌ"),
            new BanglishPattern("ngOI", "ঙ্গৈ"),
            new BanglishPattern("Ngkx", "ঙ্ক্ষ"),
            new BanglishPattern("NGc", "ঞ্চ"),
            new BanglishPattern("nch", "ঞ্ছ"),
            new BanglishPattern("njh", "ঞ্ঝ"),
            new BanglishPattern("ngh", "ঙ্ঘ"),
            new BanglishPattern("Ngk", "ঙ্ক"),
            new BanglishPattern("Ngx", "ঙ্ষ"),
            new BanglishPattern("Ngg", "ঙ্গ"),
            new BanglishPattern("Ngm", "ঙ্ম"),
            new BanglishPattern("NGj", "ঞ্জ"),
            new BanglishPattern("ndh", "ন্ধ"),
            new BanglishPattern("nTh", "ন্ঠ"),
            new BanglishPattern("NTh", "ণ্ঠ"),
            new BanglishPattern("nth", "ন্থ"),
            new BanglishPattern("nkh", "ঙ্খ"),
            new BanglishPattern("ngo", "ঙ্গ"),
            new BanglishPattern("nga", "ঙ্গা"),
            new BanglishPattern("ngi", "ঙ্গি"),
            new BanglishPattern("ngI", "ঙ্গী"),
            new BanglishPattern("ngu", "ঙ্গু"),
            new BanglishPattern("ngU", "ঙ্গূ"),
            new BanglishPattern("nge", "ঙ্গে"),
            new BanglishPattern("ngO", "ঙ্গো"),
            new BanglishPattern("NDh", "ণ্ঢ"),
            new BanglishPattern("nsh", "নশ"),
            new BanglishPattern("Ngr", "ঙর"),
            new BanglishPattern("NGr", "ঞর"),
            new BanglishPattern("ngr", "ংর"),
            new BanglishPattern("nj", "ঞ্জ"),
            new BanglishPattern("Ng", "ঙ"),
            new BanglishPattern("NG", "ঞ"),
            new BanglishPattern("nk", "ঙ্ক"),
            new BanglishPattern("ng", "ং"),
            new BanglishPattern("nn", "ন্ন"),
            new BanglishPattern("NN", "ণ্ণ"),
            new BanglishPattern("Nn", "ণ্ন"),
            new BanglishPattern("nm", "ন্ম"),
            new BanglishPattern("Nm", "ণ্ম"),
            new BanglishPattern("nd", "ন্দ"),
            new BanglishPattern("nT", "ন্ট"),
            new BanglishPattern("NT", "ণ্ট"),
            new BanglishPattern("nD", "ন্ড"),
            new BanglishPattern("ND", "ণ্ড"),
            new BanglishPattern("nt", "ন্ত"),
            new BanglishPattern("ns", "ন্স"),
            new BanglishPattern("nc", "ঞ্চ"),
            new BanglishPattern("n", "ন"),
            new BanglishPattern("N", "ণ"),
            new BanglishPattern("OI`", "ৈ"),
            new BanglishPattern("OU`", "ৌ"),
            new BanglishPattern("O`", "ো"),
            new BanglishPattern("OI", "ৈ", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> { new BanglishMatch("p", "c", true, "") }, "ঐ"),
                new BanglishRule(new List<BanglishMatch> { new BanglishMatch("p", "p", false, "") }, "ঐ")
            }),
            new BanglishPattern("OU", "ৌ", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> { new BanglishMatch("p", "c", true, "") }, "ঔ"),
                new BanglishRule(new List<BanglishMatch> { new BanglishMatch("p", "p", false, "") }, "ঔ")
            }),
            new BanglishPattern("O", "ো", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> { new BanglishMatch("p", "c", true, "") }, "ও"),
                new BanglishRule(new List<BanglishMatch> { new BanglishMatch("p", "p", false, "") }, "ও")
            }),
            new BanglishPattern("phl", "ফ্ল"),
            new BanglishPattern("pT", "প্ট"),
            new BanglishPattern("pt", "প্ত"),
            new BanglishPattern("pn", "প্ন"),
            new BanglishPattern("pp", "প্প"),
            new BanglishPattern("pl", "প্ল"),
            new BanglishPattern("ps", "প্স"),
            new BanglishPattern("ph", "ফ"),
            new BanglishPattern("fl", "ফ্ল"),
            new BanglishPattern("f", "ফ"),
            new BanglishPattern("p", "প"),
            new BanglishPattern("rri`", "ৃ"),
            new BanglishPattern("rri", "ৃ", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> { new BanglishMatch("p", "c", true, "") }, "ঋ"),
                new BanglishRule(new List<BanglishMatch> { new BanglishMatch("p", "p", false, "") }, "ঋ")
            }),
            new BanglishPattern("rrZ", "রর‍্য"),
            new BanglishPattern("rry", "রর‍্য"),
            new BanglishPattern("rZ", "র‍্য", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", false, ""),
                    new BanglishMatch("p", "e", true, "r"),
                    new BanglishMatch("p", "e", true, "y"),
                    new BanglishMatch("p", "e", true, "w"),
                    new BanglishMatch("p", "e", true, "x")
                }, "্র্য")
            }),
            new BanglishPattern("ry", "র‍্য", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", false, ""),
                    new BanglishMatch("p", "e", true, "r"),
                    new BanglishMatch("p", "e", true, "y"),
                    new BanglishMatch("p", "e", true, "w"),
                    new BanglishMatch("p", "e", true, "x")
                }, "্র্য")
            }),
            new BanglishPattern("rr", "রর", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", true, ""),
                    new BanglishMatch("s", "v", true, ""),
                    new BanglishMatch("s", "e", true, "r"),
                    new BanglishMatch("s", "p", true, "")
                }, "র্"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", false, ""),
                    new BanglishMatch("p", "e", true, "r")
                }, "্রর")
            }),
            new BanglishPattern("Rg", "ড়্গ"),
            new BanglishPattern("Rh", "ঢ়"),
            new BanglishPattern("R", "ড়"),
            new BanglishPattern("r", "র", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", false, ""),
                    new BanglishMatch("p", "e", true, "r"),
                    new BanglishMatch("p", "e", true, "y"),
                    new BanglishMatch("p", "e", true, "w"),
                    new BanglishMatch("p", "e", true, "x"),
                    new BanglishMatch("p", "e", true, "Z")
                }, "্র")
            }),
            new BanglishPattern("shch", "শ্ছ"),
            new BanglishPattern("ShTh", "ষ্ঠ"),
            new BanglishPattern("Shph", "ষ্ফ"),
            new BanglishPattern("Sch", "শ্ছ"),
            new BanglishPattern("skl", "স্ক্ল"),
            new BanglishPattern("skh", "স্খ"),
            new BanglishPattern("sth", "স্থ"),
            new BanglishPattern("sph", "স্ফ"),
            new BanglishPattern("shc", "শ্চ"),
            new BanglishPattern("sht", "শ্চ"),
            new BanglishPattern("shn", "শ্ন"),
            new BanglishPattern("shm", "শ্ম"),
            new BanglishPattern("shl", "শ্ল"),
            new BanglishPattern("Shk", "ষ্ক"),
            new BanglishPattern("ShT", "ষ্ট"),
            new BanglishPattern("ShN", "ষ্ণ"),
            new BanglishPattern("Shp", "ষ্প"),
            new BanglishPattern("Shf", "ষ্ফ"),
            new BanglishPattern("Shm", "ষ্ম"),
            new BanglishPattern("spl", "স্প্ল"),
            new BanglishPattern("sk", "স্ক"),
            new BanglishPattern("Sc", "শ্চ"),
            new BanglishPattern("sT", "স্ট"),
            new BanglishPattern("st", "স্ত"),
            new BanglishPattern("sn", "স্ন"),
            new BanglishPattern("sp", "স্প"),
            new BanglishPattern("sf", "স্ফ"),
            new BanglishPattern("sm", "স্ম"),
            new BanglishPattern("sl", "স্ল"),
            new BanglishPattern("sh", "শ"),
            new BanglishPattern("St", "শ্ত"),
            new BanglishPattern("Sn", "শ্ন"),
            new BanglishPattern("Sm", "শ্ম"),
            new BanglishPattern("Sl", "শ্ল"),
            new BanglishPattern("Sh", "ষ"),
            new BanglishPattern("s", "স"),
            new BanglishPattern("S", "শ"),
            new BanglishPattern("oo`", "ু"),
            new BanglishPattern("oo", "ু", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", true, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "উ"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "উ")
            }),
            new BanglishPattern("o`", ""),
            new BanglishPattern("oZ", "অ্য"),
            new BanglishPattern("o", "", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "v", false, ""),
                    new BanglishMatch("p", "e", true, "o")
                }, "ও"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "v", false, ""),
                    new BanglishMatch("p", "e", false, "o")
                }, "অ"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, "")
                }, "অ")
            }),
            new BanglishPattern("tth", "ত্থ"),
            new BanglishPattern("t``", "ৎ"),
            new BanglishPattern("TT", "ট্ট"),
            new BanglishPattern("Tm", "ট্ম"),
            new BanglishPattern("Th", "ঠ"),
            new BanglishPattern("tn", "ত্ন"),
            new BanglishPattern("tm", "ত্ম"),
            new BanglishPattern("th", "থ"),
            new BanglishPattern("tt", "ত্ত"),
            new BanglishPattern("T", "ট"),
            new BanglishPattern("t", "ত"),
            new BanglishPattern("aZ", "অ্যা"),
            new BanglishPattern("AZ", "অ্যা"),
            new BanglishPattern("a`", "া"),
            new BanglishPattern("A`", "া"),
            new BanglishPattern("a", "া", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "আ"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", true, ""),
                    new BanglishMatch("p", "e", true, "a"),
                    new BanglishMatch("s", "e", true, "`")
                }, "য়া"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "e", false, "a"),
                    new BanglishMatch("s", "e", true, "`")
                }, "আ")
            }),
            new BanglishPattern("i`", "ি"),
            new BanglishPattern("i", "ি", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", true, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "ই"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "ই")
            }),
            new BanglishPattern("I`", "ী"),
            new BanglishPattern("I", "ী", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", true, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "ঈ"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "ঈ")
            }),
            new BanglishPattern("u`", "ু"),
            new BanglishPattern("u", "ু", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", true, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "উ"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "উ")
            }),
            new BanglishPattern("U`", "ূ"),
            new BanglishPattern("U", "ূ", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", true, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "ঊ"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "ঊ")
            }),
            new BanglishPattern("ee`", "ী"),
            new BanglishPattern("ee", "ী", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", true, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "ঈ"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "ঈ")
            }),
            new BanglishPattern("e`", "ে"),
            new BanglishPattern("e", "ে", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", true, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "এ"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, ""),
                    new BanglishMatch("s", "e", true, "`")
                }, "এ")
            }),
            new BanglishPattern("z", "য"),
            new BanglishPattern("Z", "্য"),
            new BanglishPattern("y", "্য", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", true, ""),
                    new BanglishMatch("p", "p", true, "")
                }, "য়"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, "")
                }, "ইয়")
            }),
            new BanglishPattern("Y", "য়"),
            new BanglishPattern("q", "ক"),
            new BanglishPattern("wya", "ওয়া"),
            new BanglishPattern("wZ", "ওয়া"),
            new BanglishPattern("wy", "ওয়"),
            new BanglishPattern("w", "ও", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, ""),
                    new BanglishMatch("s", "v", false, "")
                }, "ওয়"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "v", false, ""),
                    new BanglishMatch("s", "v", false, "")
                }, "ওয়"),
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "c", false, "")
                }, "্ব")
            }),
            new BanglishPattern("x", "ক্স", new List<BanglishRule> {
                new BanglishRule(new List<BanglishMatch> {
                    new BanglishMatch("p", "p", false, "")
                }, "এক্স")
            }),
            new BanglishPattern(":`", ":"),
            new BanglishPattern(":", "ঃ"),
            new BanglishPattern("^`", "^"),
            new BanglishPattern("^", "ঁ"),
            new BanglishPattern(",,", "্‌"),
            new BanglishPattern(",", ","),
            new BanglishPattern("$", "৳"),
            new BanglishPattern("`", "")
        };
    }
}
