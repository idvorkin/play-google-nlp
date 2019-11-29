using Google.Cloud.Language.V1;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CommandLine;
using IBM.Watson.NaturalLanguageUnderstanding.v1.Model;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Watson.NaturalLanguageUnderstanding.v1;

class Options
{
    const string USE_HARDCODED_FILE = "USE_HARDCODED_FILE";
    [Option('f', "file", Default = USE_HARDCODED_FILE, Required = false, HelpText = "Input files to be processed.")]
    public string InputFile { get; set; }
    [Option('s', "stdin", Default = false, Required = false, HelpText = "Process Standard in.")]
    public bool StdIn { get; set; }
    [Option('g', "GroupByEntity", Default = true, Required = false, HelpText = "GroupByEntity")]
    public bool GroupByEntity { get; set; }
    [Option('v', "Verbose", Default = false, Required = false, HelpText = "Verbose Output")]
    public bool Verbose { get; set; }
    public bool IsUseHardcodedFile()
    {
        return this.InputFile == Options.USE_HARDCODED_FILE;
    }
}

namespace google_nlp
{
    static class Extensions
    {
        public static string ToPcnt(this float d)
        {
            return $"{(int)(d * 100):D2}";
        }
        public static string ToPcnt(this double? d)
        {
            if (d == null)
            {
                return "00";
            }
            return ((float)d).ToPcnt();
        }

        public static string Pretty(this EmotionScores es)
        {
            return $"[Joy:{es.Joy.ToPcnt()}, Sad:{es.Sadness.ToPcnt()}, Mad:{es.Anger.ToPcnt()},Fear:{es.Fear.ToPcnt()}, Disgust:{es.Disgust.ToPcnt()}]";
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
                       $"{homeDirectory}/gits/igor2/750words_new_archive/2018-12-04.md" :
                       opts.InputFile;

                  Console.WriteLine($"Running NLP on {fileToAnalyze}");
                  var textToAnalyze = File.ReadAllText(fileToAnalyze).ToLower();
                  program.InstanceMain(opts, textToAnalyze);
              }
              );
        }


        void InstanceMain(Options opts, string textToAnalyze)
        {
            // AnalyzeWithGoogle(opts, textToAnalyze);
            AnalyzeWithWatson(opts, textToAnalyze);
        }

        private void AnalyzeWithWatson(Options opts, string textToAnalyze)
        {
            var secrets = JObject.Parse(File.ReadAllText($"{homeDirectory}/gits/igor2/secretBox.json"));
            var key = secrets["IBMWatsonKeyNLU"];
            if (key == null) throw new InvalidDataException("Missing Key");
            // Console.WriteLine($"{key}");

            var authenticator = new IamAuthenticator(apikey: $"{key}");
            var service = new NaturalLanguageUnderstandingService("2019-07-12", authenticator);

            var features = new Features()
            {
                Keywords = new KeywordsOptions()
                {
                    Limit = 10,
                    Sentiment = true,
                    Emotion = true
                },
                Entities = new EntitiesOptions()
                {
                    Sentiment = true,
                    Emotion = true,
                    Limit = 100
                },
                Sentiment = new SentimentOptions()
                {
                    Document = true
                },
                Emotion = new EmotionOptions()
                {
                    Document = true
                }

            };

            var result = service.Analyze(
                features: features,
                text: textToAnalyze
                );

            var doc = result.Result;
            if (opts.Verbose)
            {
                Console.WriteLine(result.Response);
            }

            Console.WriteLine($"Overall {doc.Sentiment.Document.Label}:{doc.Sentiment.Document.Score.ToPcnt()} E:{doc.Emotion.Document.Emotion.Pretty()}");
            foreach (var e in doc.Entities)
            {
                if (e.Type != "Person" || e.Confidence < 0.50)
                {
                    continue;
                }
                Console.WriteLine($"{e.Text.PadRight(25)} - R:{e.Relevance.ToPcnt()}, C:{e.Confidence.ToPcnt()}, S:{e.Sentiment.Score.ToPcnt()} E:{e.Emotion.Pretty()}");
                // Console.WriteLine(e.Sentiment.ToString());
            }
        }

        private void AnalyzeWithGoogle(Options opts, string textToAnalyze)
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

            if (opts.GroupByEntity)
            {

                var entityGroups = rEntities.Entities.GroupBy(g => g.Name);
                foreach (var eg in entityGroups.OrderBy(eg => eg.Count()))
                {
                    Console.WriteLine($"{eg.Key}, {eg.Count()}, {eg.First().Type}");
                    foreach (var entity in eg.OrderBy(e => e.Salience))
                    {
                        Console.WriteLine($"  I:{entity.Salience} M:{entity.Sentiment.Magnitude} S:{entity.Sentiment.Score.ToPcnt()}");
                    }

                }
            }
            else
            {
                foreach (var entity in rEntities.Entities.ToList().OrderBy(e => e.Salience))
                {
                    Console.WriteLine($"{entity.Name} I:{entity.Salience} M:{entity.Sentiment.Magnitude} S:{entity.Sentiment.Score.ToPcnt()} T:{entity.Type}");
                }
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
