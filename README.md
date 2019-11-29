# Cloud Service NLP Explorations

NLP is a big field and you can either roll your own, or use a cloud provider (or the hybrid solution of having them train/host your NLP model).

This project is a place to store my cloud service explorations. Most of my pet projects are in python, so I'll use C# for this one to mix it up.

This is part of a larger nlp [exploration](https://github.com/idvorkin/techdiary/blob/master/notes/sentiment_analysis.md)


### Techincal Details

#### Google Get credentials.

1. Create creds from google console.
1. make server creds , generates a .json you need to store locally.
1. Point GOOGLE_APPLICATION_CREDENTIALS to your downloaded creds

    ```export GOOGLE_APPLICATION_CREDENTIALS=~/google-nlp/igorplaygocreds.json```

1. That's the normal way, but I want to share code between win and WSL so had to add setting environment to my c# code.
See: https://github.com/idvorkin/play-google-nlp/commit/c8efc97388943d798ab30ba1650a6099eed32e3c

#### Watson get credentials

1. Create creds from ibm console
1. Pass creds as API keys

#### From windows, edit in visual studio, run from WSL, when on *nix, edit from vim, run from *nix.

VS on C# is best programming experience - EVER. Modern language, perfect code completion, auto fix errors, great debugger. Oh My!!!

Sharing code between VS and WSL is a pain!!

- VS builds isn't compatible with how WSL builds, and if you build from WSL, you lose code completion in VS.
- Worked around by creating a seperate repo for the WSL side, and override files with symlinks to the windows side, this lets me edit in VS, and run from WSL, after I rebuild.
- Setting credentials consistely in C# in both Windows/WSL is painful so I did the work in code.
