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
            using var file = new StreamWriter(FileInfo.FullName, true);
            file.WriteLine(value);
        }

        public void Replace(string replacementValue)
        {
            File.WriteAllLines(FileInfo.FullName,
                File.ReadLines(FileInfo.FullName)
                    .Select(l => l == replacementValue ? replacementValue : l)
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
            foreach (var entry in replacementDict)
            {
                arrLine[entry.Value - 1] = arrLine[entry.Value - 1].Replace(entry.Key.Key, entry.Key.Value);
                
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
            foreach (var entry in replacementValueLine)
            {
                arrLine[entry.Value - 1] = entry.Key;
            }
            File.WriteAllLines(FileInfo.FullName, arrLine);
        }

        public void Remove(string valToRemove)
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
