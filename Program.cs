using Google.Cloud.Language.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace google_nlp
{
    class Program
    {
        static void Main(string[] args)
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", $"{home}/gits/igor2/secrets/google-nlp-igorplaygocreds.json");
            var fileToAnalyze = $"{home}/gits/igor2/750words/2018-12-04.md";
            Console.WriteLine($"Running NLP on {fileToAnalyze}");

            // The text to analyze.
            var client = LanguageServiceClient.Create();
            Document document = (new Document()
            {
                Type = Document.Types.Type.PlainText,
                Content = File.ReadAllText(fileToAnalyze).ToLower(),
            });
            var response = client.AnalyzeEntitySentiment(document);


            // var entities = response.Entities.To
            foreach (var e in response.Entities.ToList().OrderBy(e=>e.Salience))
            {
                Console.WriteLine($"{e.Name} I:{e.Salience} M:{e.Sentiment.Magnitude} S:{e.Sentiment.Score} T:{e.Type}");
            }
            var r = client.ClassifyText(document);
            foreach (var category in r.Categories.OrderBy(_=>_.Confidence))
            {
                Console.WriteLine($"{category.Name} C:{category.Confidence}");
            }
            var s = client.AnalyzeSentiment(document);
            // 
            var interestingSentances = s.Sentences.
                    Where(_ => !_.Text.Content.StartsWith("#")). // Remove markdown headers.
                    Where(_ => _.Sentiment.Magnitude != 0). // Remove things that don't have magnitude.
                    OrderBy(_ => _.Sentiment.Magnitude);
            foreach (var sentiment in interestingSentances)
            {
                Console.WriteLine($"M:{sentiment.Sentiment.Magnitude} S:{sentiment.Sentiment.Score}: {sentiment.Text.Content}");
            }
            Console.WriteLine($"Overall M:{s.DocumentSentiment.Magnitude} S:{s.DocumentSentiment.Score}");
        }
    }

}
