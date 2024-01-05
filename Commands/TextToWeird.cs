using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FruitBowlBot_v2.Commands
{
    internal class TextToWeird : IPluginCommand
    {
        public string PluginName => "say";
        public string Command => "say";
        public IEnumerable<string> Help => new[] { "!say what to say" };
        public IEnumerable<string> Aliases => new string[0];

        public bool ModOnly { get; set; } = true;
        public bool Loaded { get; set; } = true;

        SoundPlayer player = new();
        Random r = new();


        public Dictionary<string, List<string>> SoundFilePaths;
        public Dictionary<string, List<string>> SpacedSoundFilePaths;
        public static Dictionary<string, int> MissingWords;

        public class Sound
        {
            public Sound(string text, bool found, string? filepath, Range range)
            {
                Text = text;
                Found = found;
                Filepath = filepath;
                Range = range;
            }

            public string Text { get; set; }
            public bool Found { get; set; }
            public string? Filepath { get; set; }
            public Range Range { get; set; }
        }

        public void LoadSoundFilePaths(string path, string[]? allowedFileTypes = null, bool recursive = true)
        {
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
            var soundfilepaths = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);//case insensitive dictionary
            var spacedsoundfilepaths = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);//case insensitive dictionary for names with spaces
            foreach (string file in Directory.EnumerateFiles(path, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                var word = Regex.Replace(Path.GetFileNameWithoutExtension(file).ToUpper(), @"[\d-]", string.Empty);
                if (allowedFileTypes != null && !allowedFileTypes.Contains(Path.GetExtension(file).ToLower())) continue;//Skip invalid filetype
                if (word.Contains(" "))
                {
                    if (spacedsoundfilepaths.ContainsKey(word))//if word exists add filepath to list
                    {
                        spacedsoundfilepaths[word].Add(file);
                    }
                    else //if word doesn't exist create new list with filepath
                    {
                        spacedsoundfilepaths[word] = new List<string>() { file };
                    }
                }
                else
                {
                    if (soundfilepaths.ContainsKey(word))//if word exists add filepath to list
                    {
                        soundfilepaths[word].Add(file);
                    }
                    else //if word doesn't exist create new list with filepath
                    {
                        soundfilepaths[word] = new List<string>() { file };
                    }
                }
            }
            SoundFilePaths = soundfilepaths;
            SpacedSoundFilePaths = spacedsoundfilepaths;
        }

        public static string[] Sanitize(string[] input)
        {
            var str = string.Join(" ", input).Trim().Replace("\'", "").Replace(","," ").Replace(".",""); //TODO use regex later, this is temp test
            Regex rgx = new("[^a-zA-Z0-9 -/_/g]");
            str = rgx.Replace(str, "");
            return str.Split(' ');
        }


        private static IEnumerable<int> GetAllIndexes(string source, string matchString)
        {
            matchString = Regex.Escape(matchString);
            foreach (Match match in Regex.Matches(source, matchString))
            {
                yield return match.Index;
            }
        }

        public List<Sound> GetSounds(string[] input, Random rnd)
        {
            
            var result = new List<Sound>();
            string sentence = string.Join(" ", input).ToLower();
            int currentIndex = 0;
            for (int i = 0; i < input.Length; i++)
            {
                var word = input[i];
                if (SoundFilePaths.ContainsKey(word))
                {
                    var path = SoundFilePaths[word][rnd.Next(SoundFilePaths[word].Count)];
                    result.Add(new Sound(word, true, path, currentIndex..(currentIndex + word.Length)));
                }
                else
                {
                    result.Add(new Sound(word, false, null, currentIndex..(currentIndex + word.Length)));
                }
                currentIndex += word.Length + 1;
            }

            foreach (var k in SpacedSoundFilePaths)
            {
                var indexes = GetAllIndexes(sentence, k.Key.ToLower());
                foreach (var i in indexes)
                {
                    var start = i;
                    var end = i + k.Key.Length;
                    var insertIndex = result.IndexOf(result.FirstOrDefault(x => x.Range.Start.Value == start));
                    if (insertIndex < 0) continue;
                    result.RemoveAll(x => x.Range.Start.Value >= start && x.Range.End.Value <= end);
                    result.Insert(insertIndex, new Sound(k.Key, true, k.Value[rnd.Next(k.Value.Count)], start..end));
                }
            }

            //Keep only longest match - so scuffed
            var groupByMax = result
                .GroupBy(x => x.Range.Start.Value)
                .SelectMany(y => y
                .Where(z => z.Range.End.Value - z.Range.Start.Value == y
                .Max(i => i.Range.End.Value - i.Range.Start.Value)));
            return groupByMax.ToList();
        }




        public Queue<string> playlist = new();


        LibVLC libVLC = new(enableDebugLogs: false);
        MediaPlayer mediaplayer;

        public TextToWeird()
        {
            Core.Initialize();

            LoadSoundFilePaths(Path.Combine(Environment.CurrentDirectory, "Sounds"), new[] { ".wav", ".ogg", ".mp3", ".mid" });
            File.WriteAllLines("wordlist.txt", SpacedSoundFilePaths.Select(x => x.Key));
            File.AppendAllLines("wordlist.txt", SoundFilePaths.Select(x => x.Key));
            MissingWords = new Dictionary<string, int>();

        }

        public async Task<string> Action(Message message)
        {

            foreach (var s in GetSounds(Sanitize(message.Arguments.ToArray()), r))
            {
                if (s.Found)
                {
                    playlist.Enqueue(s.Filepath);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[{s.Range}]{s.Text}: {Path.GetFileName(s.Filepath)}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{s.Range}]{s.Text}: {Path.GetFileName(s.Filepath)}");

                    try
                    {
                        if (MissingWords.ContainsKey(s.Text))
                            MissingWords[s.Text]++;
                        else
                            MissingWords.Add(s.Text, 1);
                    }
                    catch (Exception a)
                    {
                        Console.WriteLine(a.ToString());
                    }
                    
                }

                Console.ForegroundColor = ConsoleColor.White;

            }


            Thread thread = new(new ThreadStart(playerthread));
            thread.Start();


            return null;
        }

        public void PlayNext()
        {
            if (playlist.Count > 0)
                mediaplayer.Play(new Media(libVLC, GetNext(), FromType.FromPath, ":no-video"));
        }

        public string GetNext()
        {

            if (playlist.Count > 0)
            {
                return playlist.Dequeue();
            }
            return "";
        }

        public void playerthread()
        {
            try
            {
                mediaplayer = new MediaPlayer(libVLC);
                // 
                try
                {
                    mediaplayer.EndReached += (sender, args) => ThreadPool.QueueUserWorkItem(_ => PlayNext());

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("This is actually by design :)");
                }
                mediaplayer.Play(new Media(libVLC, GetNext(), FromType.FromPath, ":no-video"));

            }
            catch (Exception ex)
            {
                // log errors
            }
        }

    }

}
