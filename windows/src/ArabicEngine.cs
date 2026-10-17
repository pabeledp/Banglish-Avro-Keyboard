using System;
using System.Collections.Generic;
using System.Text;

namespace Banglish.Core
{
    public class ArabicEngine
    {
        public static readonly ArabicEngine Shared = new ArabicEngine();

        public string StripDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c >= 0x064B && c <= 0x0652) continue;
                sb.Append(c);
            }
            return sb.ToString();
        }

        public string Transliterate(string rawInput)
        {
            if (string.IsNullOrEmpty(rawInput)) return "";

            int total = rawInput.Length;
            StringBuilder output = new StringBuilder();
            int cur = 0;

            while (cur < total)
            {
                bool isWordStart = (cur == 0) || (cur > 0 && (rawInput[cur - 1] == ' ' || rawInput[cur - 1] == '-'));

                // 1. Initial Vowel Handling
                if (isWordStart)
                {
                    if (cur + 1 < total && char.ToLowerInvariant(rawInput[cur]) == 'a' && char.ToLowerInvariant(rawInput[cur + 1]) == 'l')
                    {
                        output.Append("ال");
                        cur += 2;
                        continue;
                    }
                    if (rawInput[cur] == 'a')
                    {
                        output.Append("أ");
                        cur += 1;
                        continue;
                    }
                    if (rawInput[cur] == 'A' || (cur + 1 < total && char.ToLowerInvariant(rawInput[cur]) == 'a' && char.ToLowerInvariant(rawInput[cur + 1]) == 'a'))
                    {
                        output.Append("ا");
                        cur += (rawInput[cur] == 'A') ? 1 : 2;
                        continue;
                    }
                    if (char.ToLowerInvariant(rawInput[cur]) == 'i' || char.ToLowerInvariant(rawInput[cur]) == 'e')
                    {
                        output.Append("إ");
                        cur += 1;
                        continue;
                    }
                    if (char.ToLowerInvariant(rawInput[cur]) == 'u' || char.ToLowerInvariant(rawInput[cur]) == 'o')
                    {
                        output.Append("أُ");
                        cur += 1;
                        continue;
                    }
                }

                // 2. Greedy pattern matching
                bool matched = false;
                foreach (var pattern in ArabicData.Patterns)
                {
                    string pFind = pattern.Find;
                    int pLen = pFind.Length;
                    int end = cur + pLen;

                    if (end <= total)
                    {
                        string sub = rawInput.Substring(cur, pLen);
                        if (sub == pFind)
                        {
                            if (output.Length > 0 && output[output.Length - 1] == 'ل' && (pattern.Replace == "ا" || pattern.Replace == "أ"))
                            {
                                output.Remove(output.Length - 1, 1);
                                output.Append(ArabicData.LamAlif);
                            }
                            else
                            {
                                output.Append(pattern.Replace);
                            }
                            cur = end;
                            matched = true;
                            break;
                        }
                    }
                }

                if (!matched)
                {
                    output.Append(rawInput[cur]);
                    cur += 1;
                }
            }

            return output.ToString();
        }
    }
}
