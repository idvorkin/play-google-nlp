using Newtonsoft.Json.Linq;
using System;
using System.IO;
using IBM.Watson.NaturalLanguageUnderstanding.v1.Model;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Watson.NaturalLanguageUnderstanding.v1;

namespace NLP
{
    class Watson
    {
        readonly NaturalLanguageUnderstandingService Service;
        public Watson(Options opts)
        {
            var secrets = opts.Secrets();
            var key = secrets["IBMWatsonKeyNLU"];
            if (key == null) throw new InvalidDataException("Missing Key");
            Service = new NaturalLanguageUnderstandingService("2019-07-12", new IamAuthenticator(apikey: $"{key}"));
        }
        public void Analyze(Options opts, string textToAnalyze)
        {
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

            var result = Service.Analyze(
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
    }

}
