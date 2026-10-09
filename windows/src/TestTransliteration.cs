using System;
using Banglish.Core;

namespace Banglish.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string[] testPhrases = new string[]
            {
                "ami banglay gan gai",
                "amar shonar bangla",
                "kemon acho?",
                "dhopa",
                "khobor",
                "obhinondon"
            };

            Console.WriteLine("========================================");
            Console.WriteLine("Banglish Engine Windows Verification Test");
            Console.WriteLine("========================================");

            foreach (var input in testPhrases)
            {
                string output = BanglishEngine.Shared.Transliterate(input);
                Console.WriteLine("Input:  " + input);
                Console.WriteLine("Output: " + output);
                Console.WriteLine("----------------------------------------");
            }
        }
    }
}
