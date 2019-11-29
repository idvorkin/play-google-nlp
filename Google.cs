using Google.Cloud.Language.V1;
using System;
using System.Linq;

namespace NLP
{
    class Google
    {
        private void AnalyzeWithGoogle(Options opts, string textToAnalyze)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", $"{opts.HomeDirectory()}/gits/igor2/secrets/google-nlp-igorplaygocreds.json");
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
