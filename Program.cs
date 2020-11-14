using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CommandLine;
using IBM.Watson.NaturalLanguageUnderstanding.v1.Model;
using Newtonsoft.Json.Linq;

class Options
{
    const string USE_HARDCODED_FILE = "USE_HARDCODED_FILE";
    [Option('f', "file", Default = USE_HARDCODED_FILE, Required = false, HelpText = "Input files to be processed.")]
    public string InputFile { get; set; }
    [Option('s', "stdin", Default = false, Required = false, HelpText = "Process Standard in.")]
    public bool StdIn { get; set; }
    [Option('g', "GroupByEntity", Default = true, Required = false, HelpText = "GroupByEntity")]
    public bool GroupByEntity { get; set; }
    [Option('w', "Watson", Default = true, Required = false, HelpText = "Use Watson")]
    public bool Watson { get; set; }
    [Option('a', "Google", Default = false, Required = false, HelpText = "Use Google")]
    public bool Google { get; set; }
    [Option('p', "Personality", Default = false, Required = false, HelpText = "See Personality")]
    public bool Personality { get; set; }
    [Option('v', "Verbose", Default = false, Required = false, HelpText = "Verbose Output")]
    public bool Verbose { get; set; }
    public bool IsUseHardcodedFile()
    {
        return this.InputFile == Options.USE_HARDCODED_FILE;
    }
    public JObject Secrets()
    {
        // TODO Make Singleton and Use a dictionary.
        return JObject.Parse(File.ReadAllText($"{HomeDirectory()}/gits/igor2/secretBox.json"));
    }
    public string HomeDirectory()
    {
        return Environment.GetEnvironmentVariable("HOME");
    }
}

namespace NLP
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

                  var textToAnalyze  = "SHOULD_BE_REPLACED";

                  if (opts.StdIn)
                  {
                    textToAnalyze = Console.In.ReadToEnd();
                  }
                  else
                  {
                      var fileToAnalyze = opts.IsUseHardcodedFile() ?
                           $"{homeDirectory}/gits/igor2/750words_new_archive/2020-09-14.md" :
                           opts.InputFile;

                      Console.WriteLine($"Running NLP on {fileToAnalyze}");
                      textToAnalyze = File.ReadAllText(fileToAnalyze);
                  }


                  program.InstanceMain(opts, textToAnalyze);
              }
              );
        }


        void InstanceMain(Options opts, string textToAnalyze)
        {
            var w = new Watson(opts);
            if (opts.Watson)
            {
                if (opts.Personality)
                {
                    w.AnalyzePersonality(opts, textToAnalyze);
                }
                else
                {
                    w.Analyze(opts, textToAnalyze);
                }
            }
            else
            {
                var g = new Google();
                g.Analyze(opts, textToAnalyze);
            }
        }
    }
}
