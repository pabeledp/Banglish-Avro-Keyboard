using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Banglish.Voice
{
    public class GoogleSpeechClient
    {
        public static void RecognizeAsync(byte[] pcmAudio, Action<string, string> callback)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    if (pcmAudio == null || pcmAudio.Length < 3200) // Less than 100ms of audio
                    {
                        if (callback != null) callback(null, "কথা স্পষ্ট শোনা যায়নি (অডিও খুব ছোট)");
                        return;
                    }

                    string apiKey = BanglishConfig.Shared.ApiKey;
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        if (callback != null) callback(null, "Google Speech API Key সেট করা নেই");
                        return;
                    }

                    string url = "https://speech.googleapis.com/v1/speech:recognize?key=" + apiKey;

                    string base64Audio = Convert.ToBase64String(pcmAudio);

                    string lang = BanglishConfig.Shared.LanguageCode ?? "bn-BD";
                    bool autoPunct = BanglishConfig.Shared.AutoPunctuation;

                    string jsonPayload = "{\r\n" +
                        "  \"config\": {\r\n" +
                        "    \"encoding\": \"LINEAR16\",\r\n" +
                        "    \"sampleRateHertz\": 16000,\r\n" +
                        "    \"languageCode\": \"" + lang + "\",\r\n" +
                        "    \"enableAutomaticPunctuation\": " + (autoPunct ? "true" : "false") + "\r\n" +
                        "  },\r\n" +
                        "  \"audio\": {\r\n" +
                        "    \"content\": \"" + base64Audio + "\"\r\n" +
                        "  }\r\n" +
                        "}";

                    byte[] postBytes = Encoding.UTF8.GetBytes(jsonPayload);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "application/json; charset=utf-8";
                    request.ContentLength = postBytes.Length;
                    request.Timeout = 12000; // 12 seconds max

                    using (Stream requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(postBytes, 0, postBytes.Length);
                    }

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        string responseJson = reader.ReadToEnd();
                        string transcript = ExtractTranscript(responseJson);

                        if (!string.IsNullOrEmpty(transcript))
                        {
                            if (callback != null) callback(transcript.Trim(), null);
                        }
                        else
                        {
                            if (callback != null) callback(null, "কোনো শব্দ শনাক্ত করা যায়নি");
                        }
                    }
                }
                catch (WebException wex)
                {
                    string errDetail = wex.Message;
                    try
                    {
                        if (wex.Response != null)
                        {
                            using (StreamReader r = new StreamReader(wex.Response.GetResponseStream()))
                            {
                                string body = r.ReadToEnd();
                                string errMsg = ExtractJsonString(body, "message");
                                if (!string.IsNullOrEmpty(errMsg)) errDetail = errMsg;
                            }
                        }
                    }
                    catch {}

                    if (callback != null) callback(null, "সার্ভার এরর: " + errDetail);
                }
                catch (Exception ex)
                {
                    if (callback != null) callback(null, "ভুল হয়েছে: " + ex.Message);
                }
            });
        }

        private static string ExtractTranscript(string json)
        {
            // Parses "transcript": "..." from Google Cloud Speech-to-Text response
            int tIdx = json.IndexOf("\"transcript\"", StringComparison.OrdinalIgnoreCase);
            if (tIdx == -1) return null;

            int colon = json.IndexOf(':', tIdx);
            if (colon == -1) return null;

            int q1 = json.IndexOf('\"', colon);
            if (q1 == -1) return null;

            // Find matching ending quote respecting escape characters
            StringBuilder sb = new StringBuilder();
            for (int i = q1 + 1; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    char next = json[i + 1];
                    if (next == '\"') { sb.Append('\"'); i++; }
                    else if (next == '\\') { sb.Append('\\'); i++; }
                    else if (next == 'n') { sb.Append('\n'); i++; }
                    else if (next == 't') { sb.Append('\t'); i++; }
                    else if (next == 'u' && i + 5 < json.Length)
                    {
                        string hex = json.Substring(i + 2, 4);
                        int code;
                        if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out code))
                        {
                            sb.Append((char)code);
                            i += 5;
                        }
                        else
                        {
                            sb.Append(c);
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else if (c == '\"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
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
