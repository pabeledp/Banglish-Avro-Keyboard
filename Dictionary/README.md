# BanglishDictionary — Comprehensive Open-Source Bengali Lexicon & Autocorrect Database

**BanglishDictionary** is an extensive, production-grade Bengali (Bangla) lexical database and phonetic autocorrect system designed for native Bengali Input Method Editors (IMEs), natural language processing (NLP), spell checkers, and search engines.

It powers **Banglish** for macOS and Windows, and is freely available under the **MIT License** for the global Bengali software and open-source community.

---

## 📊 Dataset Overview

| Component | Entries | Description | File Path |
| :--- | :--- | :--- | :--- |
| **Comprehensive Lexicon** | **513,380+ words** | Complete Bengali vocabulary covering standard literature, classical terms, colloquial phrases, and modern loanwords | [`Resources/words.txt`](../Resources/words.txt) |
| **Phonetic Autocorrect & Loanwords** | **4,800+ mappings** | Direct phonetic & English-to-Bangla spelling corrections for loanwords, tech terms, and common orthographic ambiguities | [`Resources/autodict.txt`](../Resources/autodict.txt) |

---

## 🏛️ Integrated Sources & Citations

BanglishDictionary aggregates, curates, normalizes, and validates vocabulary from leading academic, linguistic, and open-source resources:

1. **Bangla Academy Standard & Accessible Dictionary (`accessibledictionary.gov.bd`):**
   - Modern Bangladesh Bangla Academy orthography, ensuring loanwords with short *'a'* (`æ`) and *'nk'* are standardized (e.g., `bank` $\rightarrow$ **ব্যাংক**, `tank` $\rightarrow$ **ট্যাংক**).
2. **BUET CSE NLP Research (`csebuetnlp/banglabert`):**
   - High-coverage modern Bengali vocabulary extracted from the BUET BanglaBERT corpus.
3. **LibreOffice Official Bengali Dictionary (`bn_BD.dic`):**
   - Morphologically verified Bengali lexicon developed for open-source office suites.
4. **OmicronLab Avro Phonetic Autocorrect Database:**
   - Over 4,800 phonetic corrections and transliteration rules originally pioneered by Dr. Mehdi Hasan Khan and enriched by the community.
5. **Sagor Sarker & Bengali NLP Library (`sagorbrur/Bengali-NLP-Library` & `bangla-bert-base`):**
   - Comprehensive tokenizer vocabularies and corpus-verified stems.
6. **Minhas Kamal Bengali Dictionary (`MinhasKamal/BengaliDictionary`):**
   - Conjunct combinations (যুক্তাক্ষর), frequency matrices, and character transition rules.
7. **Bangla Conversational LLM Corpus (`Bangla LLM.xlsx`):**
   - Real-world conversational, digital, and colloquial expressions used in everyday chats.

---

## 🔍 How It Works

### 1. English Loanword Transliteration Standard
Standard English words borrowed into everyday Bengali speech are mapped directly to their Bangla Academy standard spellings:
- `bank` $\rightarrow$ **ব্যাংক** (Candidate #1), **ব্যাঙ্ক** (Candidate #2)
- `tank` $\rightarrow$ **ট্যাংক**
- `rank` $\rightarrow$ **র‍্যাংক**
- `link` $\rightarrow$ **লিংক**
- `pink` $\rightarrow$ **পিংক**
- `doctor` $\rightarrow$ **ডাক্তার**
- `hospital` $\rightarrow$ **হাসপাতাল**
- `software` $\rightarrow$ **সফটওয়্যার**
- `card` $\rightarrow$ **কার্ড**
- `police` $\rightarrow$ **পুলিশ**

### 2. Phonetic Diphthongs & Y-Ambiguity
- Input like `iuai` or `ui` directly suggests **ইউআই** (UI).
- `ux` $\rightarrow$ **ইউএক্স**
- `ai` $\rightarrow$ **এআই**
- Common verbs: `dewa` / `deya` $\rightarrow$ **দেওয়া** / **দেয়া**, `jaoya` $\rightarrow$ **যাওয়া**, `khawa` $\rightarrow$ **খাওয়া**.

### 3. Smart Punctuation
- Typing `.` gives **।** (দাঁড়ি) as primary candidate and **.** (dot) as secondary candidate. Pressing dot again (`..`) directly outputs a period.

---

## 🚀 Usage in Other Projects

### Python Example:
```python
# Load lexicon
with open("Resources/words.txt", encoding="utf-8") as f:
    valid_words = set(line.strip() for line in f)

# Load autocorrect
autodict = {}
with open("Resources/autodict.txt", encoding="utf-8") as f:
    for line in f:
        k, v = line.strip().split("\t")
        autodict[k.lower()] = v

def get_suggestion(query):
    query_lower = query.lower()
    if query_lower in autodict:
        return autodict[query_lower]
    return None

print(get_suggestion("bank"))  # আউটপুট: ব্যাংক
print(get_suggestion("iuai"))  # আউটপুট: ইউআই
```

---

## 📜 License
Released under the **MIT License**. Free for academic, personal, and commercial software use.
