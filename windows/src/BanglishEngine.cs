using System;
using System.Collections.Generic;
using System.Text;

namespace Banglish.Core
{
    public class BanglishEngine
    {
        public static readonly BanglishEngine Shared = new BanglishEngine();

        private readonly HashSet<char> _vowels;
        private readonly HashSet<char> _consonants;
        private readonly HashSet<char> _caseSensitive;

        public BanglishEngine()
        {
            _vowels = new HashSet<char>(AvroData.Vowels);
            _consonants = new HashSet<char>(AvroData.Consonants);
            _caseSensitive = new HashSet<char>(AvroData.CaseSensitive);
        }

        private string FixString(string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                char lower = char.ToLowerInvariant(c);
                if (_caseSensitive.Contains(lower))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(lower);
                }
            }
            return sb.ToString();
        }

        private bool IsVowel(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return _vowels.Contains(char.ToLowerInvariant(s[0]));
        }

        private bool IsConsonant(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return _consonants.Contains(char.ToLowerInvariant(s[0]));
        }

        private bool IsPunctuation(string s)
        {
            return !IsVowel(s) && !IsConsonant(s);
        }

        private bool IsExact(string needle, string[] text, int start, int end, bool not)
        {
            bool condition = false;
            if (start >= 0 && end <= text.Length && start <= end)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = start; i < end; i++)
                {
                    sb.Append(text[i]);
                }
                condition = (sb.ToString() == needle);
            }
            return condition != not;
        }

        public string Transliterate(string rawInput)
        {
            if (string.IsNullOrEmpty(rawInput)) return "";

            string fixedStr = FixString(rawInput);
            string[] chars = new string[fixedStr.Length];
            for (int i = 0; i < fixedStr.Length; i++)
            {
                chars[i] = fixedStr[i].ToString();
            }

            int total = chars.Length;
            StringBuilder output = new StringBuilder();
            int cur = 0;

            while (cur < total)
            {
                int start = cur;
                int prev = start - 1;
                bool matched = false;

                foreach (var pattern in AvroData.Patterns)
                {
                    string pFind = pattern.Find;
                    int pFindLen = pFind.Length;
                    int end = cur + pFindLen;

                    if (end <= total)
                    {
                        StringBuilder subSb = new StringBuilder();
                        for (int i = start; i < end; i++) subSb.Append(chars[i]);
                        string sub = subSb.ToString();

                        if (sub == pFind)
                        {
                            if (pattern.Rules != null && pattern.Rules.Count > 0)
                            {
                                foreach (var rule in pattern.Rules)
                                {
                                    bool ruleMatched = true;

                                    foreach (var match in rule.Matches)
                                    {
                                        int chk = (match.Type == "s") ? end : prev;

                                        if (match.Scope == "p") // punctuation / boundary
                                        {
                                            bool cond = (chk < 0 && match.Type == "p") ||
                                                        (chk >= total && match.Type == "s") ||
                                                        (chk >= 0 && chk < total && IsPunctuation(chars[chk]));
                                            if (cond == match.Negative)
                                            {
                                                ruleMatched = false;
                                                break;
                                            }
                                        }
                                        else if (match.Scope == "v") // vowel
                                        {
                                            bool cond = (chk >= 0 && match.Type == "p" && IsVowel(chars[chk])) ||
                                                        (chk < total && match.Type == "s" && IsVowel(chars[chk]));
                                            if (cond == match.Negative)
                                            {
                                                ruleMatched = false;
                                                break;
                                            }
                                        }
                                        else if (match.Scope == "c") // consonant
                                        {
                                            bool cond = (chk >= 0 && match.Type == "p" && IsConsonant(chars[chk])) ||
                                                        (chk < total && match.Type == "s" && IsConsonant(chars[chk]));
                                            if (cond == match.Negative)
                                            {
                                                ruleMatched = false;
                                                break;
                                            }
                                        }
                                        else if (match.Scope == "e") // exact value match
                                        {
                                            int sIdx, eIdx;
                                            if (match.Type == "s")
                                            {
                                                sIdx = end;
                                                eIdx = end + match.Value.Length;
                                            }
                                            else
                                            {
                                                sIdx = start - match.Value.Length;
                                                eIdx = start;
                                            }

                                            if (!IsExact(match.Value, chars, sIdx, eIdx, match.Negative))
                                            {
                                                ruleMatched = false;
                                                break;
                                            }
                                        }
                                    }

                                    if (ruleMatched)
                                    {
                                        output.Append(rule.Replace);
                                        cur = end - 1;
                                        matched = true;
                                        break;
                                    }
                                }
                            }

                            if (matched) break;

                            output.Append(pattern.Replace);
                            cur = end - 1;
                            matched = true;
                            break;
                        }
                    }
                }

                if (!matched)
                {
                    output.Append(chars[cur]);
                }

                cur++;
            }

            return output.ToString();
        }
    }
}
