using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Porter2StemmerStandard;


namespace InvertedIndex {
    public class InvertedIndex {
        private Dictionary<string,Dictionary<string,int>> map;

        public InvertedIndex() {
            map = new Dictionary<string, Dictionary<string,int>>();
        }

        public void addOrUpdate(string word,string path) {
            // simplifies word into common form for consistancy
            word = word.simplifyWord();
            if (word == "") return;

            if (map.TryGetValue(word, out Dictionary<string, int>? docReferenceMap)) {
                if (docReferenceMap.ContainsKey(path)) {
                    docReferenceMap[path]++;
                }
                else {
                    docReferenceMap.Add(path, 1);
                }
            }
            else {
                docReferenceMap = new Dictionary<string, int>();
                map.Add(word, docReferenceMap);

                docReferenceMap.Add(path,1);
            }
        }

        public void printWord(string? word) {
            if (word == null) {
                Console.WriteLine("word does not exist in map");
                return;
            }

            word = word.simplifyWord();

            if (map.TryGetValue(word,out Dictionary<string,int>? docReferenceMap)) {
                foreach(KeyValuePair<string,int> docReference in docReferenceMap) {
                    Console.WriteLine(docReference.ToString());
                }
            }
            else {
                Console.WriteLine("word does not exist in map");
            }
        }

        public ArrayList getWord(string? word) {
            ArrayList foundReferenceArray = new ArrayList();

            if (word == null) {
                Console.WriteLine("word does not exist in map");
                return foundReferenceArray;
            }

            word = word.simplifyWord();

            if (map.TryGetValue(word,out Dictionary<string,int>? docReferenceMap)) {
                foreach (KeyValuePair<string,int> docReference in docReferenceMap) {
                    // Console.WriteLine(docReference.ToString());
                    foundReferenceArray.Add(docReference);
                }
            }
            else {
                Console.WriteLine("word does not exist in map");
                return foundReferenceArray;
            }

            IComparer comparer = new PairComparer();
            foundReferenceArray.Sort(comparer);

            return foundReferenceArray;
        }
    }

    internal class ArrayList<T> : List<KeyValuePair<string, int>>
    {
    }

    internal class PairComparer : IComparer {
         
        int IComparer.Compare(object? a, object? b)
        {
            if (a is KeyValuePair<string, int> pair1 && b is KeyValuePair<string, int> pair2) {
                return new CaseInsensitiveComparer().Compare(pair2.Value, pair1.Value);
            }
            return 0;
        }
    }

    public static class StringExtension {
        private static readonly string[] stopWords = {"a", "an", "and", "as", "at", "by", "for", "in", "is", "it", "of", "on", "the", "to", "was", "were"};
        public static string simplifyWord(this string word) {
            // Convert string to lowercase for consistancy
            word = word.ToLower();

            // Use a regular expression to remove non-alphabetic characters
            word = Regex.Replace(word, @"[^a-z]", "");


            // Using PorterStemmer Library to simplifiy
            // A stemmer helps convert similar words into a common form so that they can be accurately compared, regardless of tense/part of speech/etc.
            EnglishPorter2Stemmer stemmer = new();

            string stemmedWord = stemmer.Stem(word).Value;

            // filter out words which dont carry significant meaning
            // i.e. "a","and","as"...
            if (stopWords.Contains(stemmedWord)) return "";

            return stemmedWord;
        }
    }

    public class Program {
        static void readFileToInvertedIndex(string path,InvertedIndex invertedIndex) {
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader(path);
                //Read the first line of text
                string? line = sr.ReadLine();
                //Continue to read until you reach end of file
                while (line != null)
                {
                    foreach(string word in line.Split(" ")) {
                        invertedIndex.addOrUpdate(word,path);
                    }
                    //Read the next line
                    line = sr.ReadLine();
                }
                //close the file
                sr.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception when reading file: " + e.Message);
            }
        }

        static void Main(string[] args) {
            InvertedIndex search = new InvertedIndex();

            foreach(string filepath in Directory.GetFiles("./documents")) {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();

                readFileToInvertedIndex(filepath,search);

                stopwatch.Stop();

                long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                Console.WriteLine($"Load file into inverted index {filepath} execution time: {elapsedMilliseconds} milliseconds");
            }

            string? input = "";
            while (input != "exit") {
                Console.Write("Enter word to search: ");

                input = Console.ReadLine();

                ArrayList foundReferenceArray = search.getWord(input);

                foreach (KeyValuePair<string, int> pair in foundReferenceArray) {
                        Console.ForegroundColor = ConsoleColor.Green;
                        string key = pair.Key;
                        string padding = new string(' ', Math.Max(0, 30 - key.Length)); // Adjust the total width as needed
                        Console.Write("".PadLeft(3) + key + padding);
                        Console.ResetColor();

                        Console.Write(" --- with ");

                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(pair.Value);
                        Console.ResetColor();

                        Console.WriteLine(" occurrences");
                }
            }
        }
    }
}