import Foundation

/// Official Avro Phonetic Transliteration Engine in Swift
public final class BanglishEngine {
    public static let shared = BanglishEngine()

    public init() {}

    private let vowels = Set(AvroData.vowels.map { String($0).lowercased() })
    private let consonants = Set(AvroData.consonants.map { String($0).lowercased() })
    private let caseSensitive = Set(AvroData.caseSensitive.map { String($0).lowercased() })

    private func fixString(_ input: String) -> String {
        var fixed = ""
        for char in input {
            let s = String(char)
            let lower = s.lowercased()
            if caseSensitive.contains(lower) {
                fixed.append(s)
            } else {
                fixed.append(lower)
            }
        }
        return fixed
    }

    private func isVowel(_ s: String) -> Bool {
        return vowels.contains(s.lowercased())
    }

    private func isConsonant(_ s: String) -> Bool {
        return consonants.contains(s.lowercased())
    }

    private func isPunctuation(_ s: String) -> Bool {
        return !isVowel(s) && !isConsonant(s)
    }

    // Matches JS: isExact: function(needle,heystack,start,end,not){return(start>=0&&end<=heystack.length&&heystack.substring(start,end)===needle)^not}
    private func isExact(_ needle: String, in text: [String], start: Int, end: Int, not: Bool) -> Bool {
        let condition: Bool
        if start >= 0 && end <= text.count && start <= end {
            let sub = text[start..<end].joined()
            condition = (sub == needle)
        } else {
            condition = false
        }
        return condition != not
    }

    /// Transliterate phonetic Banglish text to Bangla Unicode
    public func transliterate(_ rawInput: String) -> String {
        guard !rawInput.isEmpty else { return "" }

        let fixedStr = fixString(rawInput)
        let chars = fixedStr.map { String($0) }
        let total = chars.count

        var output = ""
        var cur = 0

        while cur < total {
            let start = cur
            let prev = start - 1
            var matched = false

            for pattern in AvroData.patterns {
                let pFind = pattern.find
                let pFindLen = pFind.count
                let end = cur + pFindLen

                if end <= total {
                    let sub = chars[start..<end].joined()
                    if sub == pFind {
                        // Check rules if any
                        if let rules = pattern.rules, !rules.isEmpty {
                            for rule in rules {
                                var ruleMatched = true

                                for match in rule.matches {
                                    let chk = (match.type == "s") ? end : prev

                                    if match.scope == "p" { // punctuation / boundary
                                        let cond = (chk < 0 && match.type == "p") ||
                                                   (chk >= total && match.type == "s") ||
                                                   (chk >= 0 && chk < total && isPunctuation(chars[chk]))
                                        if cond == match.negative {
                                            ruleMatched = false
                                            break
                                        }
                                    } else if match.scope == "v" { // vowel
                                        let cond = (chk >= 0 && match.type == "p" && isVowel(chars[chk])) ||
                                                   (chk < total && match.type == "s" && isVowel(chars[chk]))
                                        if cond == match.negative {
                                            ruleMatched = false
                                            break
                                        }
                                    } else if match.scope == "c" { // consonant
                                        let cond = (chk >= 0 && match.type == "p" && isConsonant(chars[chk])) ||
                                                   (chk < total && match.type == "s" && isConsonant(chars[chk]))
                                        if cond == match.negative {
                                            ruleMatched = false
                                            break
                                        }
                                    } else if match.scope == "e" { // exact value match
                                        let sIdx: Int
                                        let eIdx: Int
                                        if match.type == "s" {
                                            sIdx = end
                                            eIdx = end + match.value.count
                                        } else {
                                            sIdx = start - match.value.count
                                            eIdx = start
                                        }
                                        if !isExact(match.value, in: chars, start: sIdx, end: eIdx, not: match.negative) {
                                            ruleMatched = false
                                            break
                                        }
                                    }
                                }

                                if ruleMatched {
                                    output.append(rule.replace)
                                    cur = end - 1
                                    matched = true
                                    break
                                }
                            }
                        }

                        if matched { break }

                        // Base pattern replacement
                        output.append(pattern.replace)
                        cur = end - 1
                        matched = true
                        break
                    }
                }
            }

            if !matched {
                output.append(chars[cur])
            }

            cur += 1
        }

        return output
    }
}
