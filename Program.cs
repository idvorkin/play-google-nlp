using Google.Cloud.Language.V1;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
// using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1.Model;


namespace google_nlp
{
    class Program
    {
        // Sharing credentials between Windows and Unix is a pain. Do it via C# for now.
        static string homeDirectory = Environment.GetEnvironmentVariable("HOME");
        static void Main(string[] args)
        {

            var fileToAnalyze = $"{homeDirectory}/gits/igor2/750words/2018-12-04.md";
            Console.WriteLine($"Running NLP on {fileToAnalyze}");

            AnalyzeWithGoogle(fileToAnalyze);
            AnalyzeWithWatson(fileToAnalyze);
        }
        private static void AnalyzeWithWatson(string fileToAnalyze)
        {
            var secrets = JObject.Parse(File.ReadAllText($"{homeDirectory}/gits/igor2/secretBox.json"));
            var key = secrets["IBMWatsonKey"];
            if (key == null) throw new InvalidDataException("Missing Key");

            /*
            var iamAssistantTokenOptions = new TokenOptions()
            {
                IamApiKey = "<iam-apikey>",
                ServiceUrl = "<service-endpoint>"
            };
            */

            //var nluClient = new NaturalLanguageUnderstandingService();
            //var _assistant = new AssistantService(iamAssistantTokenOptions, "<version-date>");
        }

        private static void AnalyzeWithGoogle(string fileToAnalyze)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", $"{homeDirectory}/gits/igor2/secrets/google-nlp-igorplaygocreds.json");
            var nlpClient = LanguageServiceClient.Create();
            // The text to analyze.
            Document textToAnalyze = (new Document()
            {
                Type = Document.Types.Type.PlainText,
                Content = File.ReadAllText(fileToAnalyze).ToLower(),
            });

            var rEntities = nlpClient.AnalyzeEntitySentiment(textToAnalyze);
            foreach (var entity in rEntities.Entities.ToList().OrderBy(e => e.Salience))
            {
                Console.WriteLine($"{entity.Name} I:{entity.Salience} M:{entity.Sentiment.Magnitude} S:{entity.Sentiment.Score} T:{entity.Type}");
            }
            var rClassify = nlpClient.ClassifyText(textToAnalyze);
            foreach (var category in rClassify.Categories.OrderBy(_ => _.Confidence))
            {
                Console.WriteLine($"{category.Name} C:{category.Confidence}");
            }

            var rSentiment = nlpClient.AnalyzeSentiment(textToAnalyze);
            var interestingSentances = rSentiment.Sentences.
                    Where(_ => !_.Text.Content.StartsWith("#")). // Remove markdown headers.
                    Where(_ => _.Sentiment.Magnitude != 0). // Remove things that don't have magnitude.
                    OrderBy(_ => _.Sentiment.Magnitude);

            foreach (var sentiment in interestingSentances)
            {
                Console.WriteLine($"M:{sentiment.Sentiment.Magnitude} S:{sentiment.Sentiment.Score}: {sentiment.Text.Content}");
            }
            Console.WriteLine($"Overall M:{rSentiment.DocumentSentiment.Magnitude} S:{rSentiment.DocumentSentiment.Score}");
        }
    }

}
