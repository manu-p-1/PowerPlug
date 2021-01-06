using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerPlug.PowerPlugFile
{
    public class PowerPlugFileBase
    {
        public FileInfo FileInfo { get; }

        public PowerPlugFileBase(string path)
        {
            FileInfo = new FileInfo(path);
        }

        public PowerPlugFileBase(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
        }
        public void WriteLine(string value)
        {
            var x = File.ReadLines(FileInfo.FullName).Last();
            using var file = new StreamWriter(FileInfo.FullName, true);

            if (x != string.Empty)
            {
                file.WriteLine(Environment.NewLine);
                
            }
            file.WriteLine(value);
        }
        
        public void Replace(string oldValue, string replacementValue)
        {
            var text = File.ReadAllText(FileInfo.FullName);
            text = text.Replace(oldValue, replacementValue);
            File.WriteAllText(FileInfo.FullName, text);
        }

        public void ReplaceFromEachLine(string oldValue, string replacementValue)
        {
            File.WriteAllLines(FileInfo.FullName,
                File.ReadLines(FileInfo.FullName)
                    .Select(l => l == oldValue ? replacementValue : l)
                    .ToList());
        }

        public void ReplaceInLine(string value, string replacementValue, int line)
        {
            ReplaceInLines(new Dictionary<KeyValuePair<string, string>, int>
            {
                {
                    new KeyValuePair<string, string>(value,
                        replacementValue),
                    line
                }
            });
        }

        public void ReplaceInLines(Dictionary<KeyValuePair<string, string>, int> replacementDict)
        {
            var arrLine = File.ReadAllLines(FileInfo.FullName);
            foreach (var ((key, s), value) in replacementDict)
            {
                arrLine[value - 1] = arrLine[value - 1].Replace(key, s);
                
            }
            File.WriteAllLines(FileInfo.FullName, arrLine);
        }

        public void ReplaceLine(string replacementValue, int line)
        {
            var arrLine = File.ReadAllLines(FileInfo.FullName);
            arrLine[line - 1] = replacementValue;
            File.WriteAllLines(FileInfo.FullName, arrLine);
        }

        public void ReplaceLines(Dictionary<string, int> replacementValueLine)
        {
            var arrLine = File.ReadAllLines(FileInfo.FullName);
            foreach (var (key, value) in replacementValueLine)
            {
                arrLine[value - 1] = key;
            }
            File.WriteAllLines(FileInfo.FullName, arrLine);
        }

        public void RemoveFromEachLine(string valToRemove)
        {
            File.WriteAllLines(FileInfo.FullName,
                File.ReadLines(FileInfo.FullName)
                    .Where(l => l != valToRemove)
                    .ToList());
        }

        public void RemoveLine(int line)
        {
            RemoveLines(line);
        }

        public void RemoveLines(params int[] lines)
        {
            var fileAsList = File.ReadAllLines(FileInfo.FullName).ToList();
            foreach (var line in lines)
            {
                fileAsList.RemoveAt(line - 1);
            }
            File.WriteAllLines(FileInfo.FullName, fileAsList.ToArray());
        }

        public int FindInFile(Func<string, bool> predicate)
        {
            using var file = new StreamReader(FileInfo.FullName, true);
            string line;
            var count = 1;

            while ((line = file.ReadLine()) != null)
            {
                if (predicate(line))
                {
                    return count;
                }
                count++;
            }

            return -1;
        }

        public string GetValueAtLine(int line) => File.ReadAllLines(FileInfo.FullName)[line - 1].Trim();
    }
}
