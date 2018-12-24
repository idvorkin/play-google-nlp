using Google.Cloud.Language.V1;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommandLine;

// using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1.Model;
class Options
{
    const string USE_HARDCODED_FILE = "USE_HARDCODED_FILE";
    [Option('f', "file",  Default=USE_HARDCODED_FILE, Required = false, HelpText = "Input files to be processed.")]
    public string InputFile { get; set; }
    [Option('s', "stdin", Default = false, Required = false, HelpText = "Process Standard in.")]
    public bool StdIn { get; set; }
    public bool IsUseHardcodedFile()
    {
        return this.InputFile == Options.USE_HARDCODED_FILE;
    }
}

namespace google_nlp
{
    static class Extensions
    {
        public static int ToPcnt(this float d)
        {
            return (int)(d * 100);
        }
    }

    class Program
    {
        // Sharing credentials between Windows and Unix is a pain. Do it via C# for now.
        static string homeDirectory = Environment.GetEnvironmentVariable("HOME");
        static void Main(string[] args)
        {
            var program = new Program();
            Parser.Default.ParseArguments<Options>(args)
              .WithParsed<Options>(opts =>
              {
                 var fileToAnalyze = opts.IsUseHardcodedFile() ?
                      $"{homeDirectory}/gits/igor2/750words/2018-12-04.md" :
                      opts.InputFile;

                  Console.WriteLine($"Running NLP on {fileToAnalyze}");
                  var textToAnalyze = File.ReadAllText(fileToAnalyze).ToLower();
                  program.InstanceMain(textToAnalyze);
              }
              );
        }


        void InstanceMain(string textToAnalyze)
        {
            AnalyzeWithGoogle(textToAnalyze);
            AnalyzeWithWatson(textToAnalyze);
        }

        private void AnalyzeWithWatson(string textToAnalyze)
        {
            var secrets = JObject.Parse(File.ReadAllText($"{homeDirectory}/gits/igor2/secretBox.json"));
            var key = secrets["IBMWatsonKey"];
            if (key == null) throw new InvalidDataException("Missing Key");

            // Can't import watson libraries - excited for when can do that.
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


        private void AnalyzeWithGoogle(string textToAnalyze)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", $"{homeDirectory}/gits/igor2/secrets/google-nlp-igorplaygocreds.json");
            var nlpClient = LanguageServiceClient.Create();
            // The text to analyze.
            Document docToAnalyze = (new Document()
            {
                Type = Document.Types.Type.PlainText,
                Content = textToAnalyze
            });

            var rEntities = nlpClient.AnalyzeEntitySentiment(docToAnalyze);
            foreach (var entity in rEntities.Entities.ToList().OrderBy(e => e.Salience))
            {
                Console.WriteLine($"{entity.Name} I:{entity.Salience} M:{entity.Sentiment.Magnitude} S:{entity.Sentiment.Score.ToPcnt()} T:{entity.Type}");
            }
            var rClassify = nlpClient.ClassifyText(docToAnalyze);
            foreach (var category in rClassify.Categories.OrderBy(_ => _.Confidence))
            {
                Console.WriteLine($"{category.Name} C:{category.Confidence}");
            }

            var rSentiment = nlpClient.AnalyzeSentiment(docToAnalyze);
            var interestingSentances = rSentiment.Sentences.
                    Where(_ => !_.Text.Content.StartsWith("#")). // Remove markdown headers.
                    Where(_ => _.Sentiment.Magnitude != 0). // Remove things that don't have magnitude.
                    Where(_ => _.Text.Content.Split().Length > 2). // Remove sentances less then length 3.
                    OrderBy(_ => _.Sentiment.Magnitude);

            foreach (var sentiment in interestingSentances)
            {
                Console.WriteLine($"M:{sentiment.Sentiment.Magnitude} S:{sentiment.Sentiment.Score.ToPcnt()}: {sentiment.Text.Content}");
            }
            Console.WriteLine($"Overall M:{rSentiment.DocumentSentiment.Magnitude} S:{rSentiment.DocumentSentiment.Score.ToPcnt()}");
        }
    }

}
