using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Porter2StemmerStandard;


namespace InvertedIndex {
    public class OldInvertedIndex {
        // and old version of the inverted index which sorts the results during a query
        //      instead of during insert
        private Dictionary<string,Dictionary<string,int>> map;

        public OldInvertedIndex() {
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

    public class InvertedIndex {
        private Dictionary<string,ArrayList<KeyValuePair<string,int>>> map;

        public InvertedIndex() {
            map = new Dictionary<string, ArrayList<KeyValuePair<string,int>>>();
        }

        public void addOrUpdate(string word,string path) {
            // simplifies word into common form for consistancy
            word = word.simplifyWord();
            if (word == "") return;

            if (map.TryGetValue(word, out ArrayList<KeyValuePair<string,int>>? docReferenceList)) {
                // loop until the doc reference is found.  If ever
                for (int i = 0; i < docReferenceList.Count; i++) {
                    if (docReferenceList[i].Key == path) {
                        // if the current path is found in the array, update its count 
                        docReferenceList[i] = new KeyValuePair<string,int>(docReferenceList[i].Key,docReferenceList[i].Value + 1);

                        // now sort array to account for changes
                        for (int j = i; j > 0 && docReferenceList[j].Value > docReferenceList[j - 1].Value; j--) {
                            // swap values
                            KeyValuePair<string,int> temp = new KeyValuePair<string,int>(docReferenceList[j].Key,docReferenceList[j].Value);
                            docReferenceList[j] = new KeyValuePair<string,int>(docReferenceList[j-1].Key,docReferenceList[j-1].Value);
                            docReferenceList[j-1] = new KeyValuePair<string,int>(temp.Key,temp.Value);
                        }

                        break;
                    }
                    else if (i == docReferenceList.Count - 1) {
                        // if at the end of the list we know that a reference to the document doesn't exist so we can add it to the end
                        docReferenceList.Add(new KeyValuePair<string,int>(path, 1));
                        // since the count is 1, and its at the end, we wont have to sort
                    }
                }
            }
            else {
                docReferenceList = new ArrayList<KeyValuePair<string,int>>();
                map.Add(word, docReferenceList);

                docReferenceList.Add(new KeyValuePair<string,int>(path,1));
            }
        }

        public void printWord(string? word) {
            if (word == null) {
                Console.WriteLine("word does not exist in map");
                return;
            }

            word = word.simplifyWord();

            if (map.TryGetValue(word,out ArrayList<KeyValuePair<string,int>>? docReferenceList)) {
                foreach(KeyValuePair<string,int> docReference in docReferenceList) {
                    Console.WriteLine(docReference.ToString());
                }
            }
            else {
                Console.WriteLine("word does not exist in map");
            }
        }

        public List<KeyValuePair<string, int>> getWord(string? word) {
            if (word == null) {
                Console.WriteLine("word does not exist in map");
                return new List<KeyValuePair<string, int>>();
            }

            word = word.simplifyWord();

            if (map.TryGetValue(word, out ArrayList<KeyValuePair<string, int>>? docReferenceList)) {
                return docReferenceList;
            }
            else {
                Console.WriteLine("word does not exist in map");
                return new List<KeyValuePair<string, int>>();
            }
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

    // public static class ArrayListExtension {
    //     public static bool containsAndGetIndex(this ArrayList list, string key, ref int index) {
    //         for (int i = 0; i < list.Count; i++) {
    //             if (list[i] is Tuple<string,int> pair && pair.Item1 == key) {
    //                 index = i;
    //                 return true;
    //             }
    //         }
    //         return false;
    //     }
    // }

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

                List<KeyValuePair<string,int>> foundReferenceArray = search.getWord(input);

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