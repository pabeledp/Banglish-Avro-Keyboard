using System;
using System.IO;
using System.Text;

namespace Banglish.Voice
{
    public class BanglishConfig
    {
        private static BanglishConfig _instance;
        public static BanglishConfig Shared
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        public string ApiKey { get; set; }
        public string LanguageCode { get; set; }
        public bool AutoPunctuation { get; set; }

        public BanglishConfig()
        {
            ApiKey = "AIzaSyBnwV6bI_pgBrVQxUW64mwgfR7sHy838Oc";
            LanguageCode = "bn-BD";
            AutoPunctuation = true;
        }

        private static string ConfigPath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Banglish");
                if (!Directory.Exists(dir))
                {
                    try { Directory.CreateDirectory(dir); } catch {}
                }
                return Path.Combine(dir, "config.json");
            }
        }

        public static BanglishConfig Load()
        {
            var config = new BanglishConfig();
            try
            {
                string path = ConfigPath;
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    string key = ExtractJsonString(json, "apiKey");
                    if (!string.IsNullOrEmpty(key)) config.ApiKey = key;

                    string lang = ExtractJsonString(json, "languageCode");
                    if (!string.IsNullOrEmpty(lang)) config.LanguageCode = lang;
                }
                else
                {
                    config.Save();
                }
            }
            catch {}
            return config;
        }

        public void Save()
        {
            try
            {
                string json = "{\r\n" +
                              "  \"apiKey\": \"" + ApiKey + "\",\r\n" +
                              "  \"languageCode\": \"" + LanguageCode + "\",\r\n" +
                              "  \"autoPunctuation\": " + (AutoPunctuation ? "true" : "false") + "\r\n" +
                              "}";
                File.WriteAllText(ConfigPath, json, Encoding.UTF8);
            }
            catch {}
        }

        private static string ExtractJsonString(string json, string prop)
        {
            int idx = json.IndexOf("\"" + prop + "\"", StringComparison.OrdinalIgnoreCase);
            if (idx == -1) return null;
            int colon = json.IndexOf(':', idx);
            if (colon == -1) return null;
            int q1 = json.IndexOf('\"', colon);
            if (q1 == -1) return null;
            int q2 = json.IndexOf('\"', q1 + 1);
            if (q2 == -1) return null;
            return json.Substring(q1 + 1, q2 - q1 - 1);
        }
    }
}
