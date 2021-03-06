﻿using Newtonsoft.Json.Linq;
using System;
using System.IO;
using IBM.Watson.NaturalLanguageUnderstanding.v1.Model;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Watson.NaturalLanguageUnderstanding.v1;
using IBM.Watson.PersonalityInsights.v3;
using IBM.Watson.PersonalityInsights.v3.Model;
using System.Collections.Generic;
using System.Linq;

namespace NLP
{
    class Watson
    {
        readonly NaturalLanguageUnderstandingService NLUService;
        readonly PersonalityInsightsService PersonalityService;
        public Watson(Options opts)
        {
            var secrets = opts.Secrets();

            var keyNLU = secrets["IBMWatsonKeyNLU"];
            if (keyNLU == null) throw new InvalidDataException("Missing NLU Key");
            NLUService = new NaturalLanguageUnderstandingService("2020-08-01", new IamAuthenticator(apikey: $"{keyNLU}"));

            var keyPersonality = secrets["IBMWatsonKeyPersonality"];
            if (keyPersonality == null) throw new InvalidDataException("Missing Personality Key");
            PersonalityService = new PersonalityInsightsService("2020-08-01", new IamAuthenticator(apikey: $"{keyPersonality}"));
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

            var result = NLUService.Analyze(
                        features: features,
                        text: textToAnalyze
                        );

            var doc = result.Result;
            if (opts.Verbose)
            {
                Console.WriteLine(result.Response);
            }

            Console.WriteLine($"Overall {doc.Sentiment.Document.Label}:{doc.Sentiment.Document.Score.ToPcnt()} E:{doc.Emotion.Document.Emotion.Pretty()}");
            // var interestingEntities = doc.Entities.Where(e => e.Type == "Person" && e.Confidence > 0.5);
            var interestingEntities = doc.Entities.Where(e => e.Confidence > 0.5 && !"Quantity Hashtag".Split().Contains(e.Type));
            interestingEntities.Take(10).ToList().ForEach(e =>
            Console.WriteLine($"{e.Text.PadRight(25)} - R:{e.Relevance.ToPcnt()}, C:{e.Confidence.ToPcnt()}, S:{e.Sentiment.Score.ToPcnt()} E:{e.Emotion.Pretty()}")
            );
        }
        public void AnalyzePersonality(Options option, string content)
        {
            // TODO Build from
            var personalityContents = new Content()
            {
                ContentItems = new List<ContentItem>{
                    new ContentItem()
                    {
                        Content = content
                    }

                }
            };

            var result = PersonalityService.Profile(
                content: personalityContents,
                contentType: "application/json",
                rawScores: true,
                consumptionPreferences: true
                );
            if (option.Verbose)
            {
                Console.WriteLine(result.Response);
            }
            result.Result.ConsumptionPreferences?.SelectMany(cp => cp.ConsumptionPreferences).ToList()
            .ForEach(cp => Console.WriteLine($"CP:{cp.Name}: {cp.Score.ToPcnt()}"));

            result.Result.Behavior?.ForEach(b =>
            Console.WriteLine($"B:{b.Name}:{b.Percentage.ToPcnt()}")
            );

            result.Result.Personality.ForEach(p =>
            Console.WriteLine($"P:{p.Name}:{p.Percentile.ToPcnt()} Significant:{p.Significant}")
            );



            Console.WriteLine($"Analyzed Words: {result.Result.WordCount}");
        }
    }

}
