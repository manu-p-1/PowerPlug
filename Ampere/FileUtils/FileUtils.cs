using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ampere.Base.Attributes;

namespace Ampere.FileUtils
{
    /// <summary>
    /// A static class for File utility functions
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// Appends a string value into the file.
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to write the value to</param>
        /// <param name="value">The string value to write</param>
        public static void WriteLine(FileInfo fileInfo, string value)
        {
            var x = File.ReadLines(fileInfo.FullName).Last();
            using var file = new StreamWriter(fileInfo.FullName, true);
            
            if (x != string.Empty)
            {
                file.WriteLine(Environment.NewLine);
                
            }
            file.WriteLine(value);
        }

        /// <summary>
        /// Replaces all instances of a specific value from a file with another replacement value.
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to write the value to</param>
        /// <param name="oldValue">The value to replace</param>
        /// <param name="replacementValue">The replacement value</param>
        public static void ReplaceAll(FileInfo fileInfo, string oldValue, string replacementValue)
        {
            var text = File.ReadAllText(fileInfo.FullName);
            text = text.Replace(oldValue, replacementValue);
            File.WriteAllText(fileInfo.FullName, text);
        }

        /// <summary>
        /// Replaces all instances of a specific value from a file with another replacement value if and only if
        /// the old value is solely in one line.
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to write the value to</param>
        /// <param name="oldValue">The value to replace</param>
        /// <param name="replacementValue">The replacement value</param>
        public static void ReplaceAllByLine(FileInfo fileInfo, string oldValue, string replacementValue)
        {
            File.WriteAllLines(fileInfo.FullName,
                File.ReadLines(fileInfo.FullName)
                    .Select(l => l == oldValue ? replacementValue : l)
                    .ToList());
        }

        /// <summary>
        /// Replaces all instances of a specific value from a file with another replacement value from a specified line
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to write the value to</param>
        /// <param name="oldValue">The value to replace</param>
        /// <param name="replacementValue">The replacement value</param>
        /// <param name="line">The line number to replace from</param>
        public static void ReplaceAllInLine(FileInfo fileInfo, string oldValue, string replacementValue, int line)
        {
            ReplaceInLines(fileInfo,new Dictionary<KeyValuePair<string, string>, int>
            {
                {
                    new KeyValuePair<string, string>(oldValue,
                        replacementValue),
                    line
                }
            });
        }

        /// <summary>
        /// Replaces all instances of a specific value from a file with another replacement value from a specified line.
        /// This overload facilitates the replacement through a Dictionary where the key's is an instance of
        /// <see cref="KeyValuePair{TKey,TValue}"/> and the value is an int. This allows for unique replacements to occur
        /// in more than one line
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to write the value to</param>
        /// <param name="replacementDict">A Dictionary of replacement values and line numbers</param>
        public static void ReplaceInLines(FileInfo fileInfo, Dictionary<KeyValuePair<string, string>, int> replacementDict)
        {
            var arrLine = File.ReadAllLines(fileInfo.FullName);
            foreach (var ((key, s), value) in replacementDict)
            {
                arrLine[value - 1] = arrLine[value - 1].Replace(key, s);
                
            }
            File.WriteAllLines(fileInfo.FullName, arrLine);
        }

        /// <summary>
        /// Replaces an entire line with a replacement value.
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to write the value to</param>
        /// <param name="replacementValue">The replacement value</param>
        /// <param name="line">The line number to replace from</param>
        public static void ReplaceLine(FileInfo fileInfo, string replacementValue, int line)
        {
            ReplaceLines(fileInfo, new Dictionary<string, int> {{replacementValue, line}});
        }

        /// <summary>
        /// Replace an entire line with a replacement value. This overload uses a Dictionary of replacement values
        /// and line numbers to replace more than one line. 
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to write the value to</param>
        /// <param name="replacementValueLine">A Dictionary of replacement values and line number</param>
        public static void ReplaceLines(FileInfo fileInfo, Dictionary<string, int> replacementValueLine)
        {
            var arrLine = File.ReadAllLines(fileInfo.FullName);
            foreach (var (key, value) in replacementValueLine)
            {
                arrLine[value - 1] = key;
            }
            File.WriteAllLines(fileInfo.FullName, arrLine);
        }

        /// <summary>
        /// Removes all instances of a specific value from a file if and only if the value is solely in one line.
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to write the value to</param>
        /// <param name="valToRemove">The value to remove</param>
        public static void RemoveFromEachLine(FileInfo fileInfo, string valToRemove)
        {
            File.WriteAllLines(fileInfo.FullName,
                File.ReadLines(fileInfo.FullName)
                    .Where(l => l != valToRemove)
                    .ToList());
        }

        /// <summary>
        /// Removes a specific line number from a file.
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to write the value to</param>
        /// <param name="line">The line number to remove</param>
        public static void RemoveLine(FileInfo fileInfo, int line)
        {
            RemoveLines(fileInfo, line);
        }

        /// <summary>
        /// Removes a variable argument number of lines from a file.
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to write the value to</param>
        /// <param name="lines">The line numbers to remove</param>
        public static void RemoveLines(FileInfo fileInfo, params int[] lines)
        {
            var fileAsList = File.ReadAllLines(fileInfo.FullName).ToList();
            foreach (var line in lines)
            {
                fileAsList.RemoveAt(line - 1);
            }
            File.WriteAllLines(fileInfo.FullName, fileAsList.ToArray());
        }

        /// <summary>
        /// Returns the line of the matched predicate in the file. If the predicate is not found,
        /// -1 is returned.
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to read from</param>
        /// <param name="predicate">The function predicate to find in the file</param>
        /// <returns></returns>
        public static int FindInFile(FileInfo fileInfo, Func<string, bool> predicate)
        {
            using var file = new StreamReader(fileInfo.FullName, true);
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

        /// <summary>
        /// Returns the value found at a line number.
        /// </summary>
        /// <param name="fileInfo">The FileInfo instance to read from</param>
        /// <param name="line">The line number to find</param>
        /// <returns></returns>
        public static string GetValueAtLine(FileInfo fileInfo, int line) => 
            File.ReadAllLines(fileInfo.FullName)[line - 1].Trim();

        /// <summary>
        /// Returns the size of a directory in bytes, given an abstract file path.
        /// </summary>
        /// <param name="dirPath">The path to the directory</param>
        /// <returns>The size of the directory in bytes</returns>
        [Beta]
        public static long GetDirectorySize(this string dirPath)
        {
            var fiArr = new DirectoryInfo(dirPath).GetFiles();
            var diArr = new DirectoryInfo(dirPath).GetDirectories();

            long length = fiArr.Sum(indv => indv.Length);

            length += diArr.Sum(indv => GetDirectorySize(indv.FullName));
            return length;
        }

        /// <summary>
        /// Returns the size of file in bytes, given an abstract file path.
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The size of the file in bytes</returns>
        [Beta]
        public static long GetFileSize(this string filePath) => new FileInfo(filePath).Length;

        /// <summary>
        /// Returns a pathname to the root directory of the System.
        /// </summary>
        /// <returns>A pathname to the root directory of the System</returns>
        [Beta]
        public static string GetRootPath() => Path.GetPathRoot(Environment.SystemDirectory);

        /// <summary>
        /// Returns a pathname to the user's profile folder.
        /// </summary>
        /// <returns>A pathname to the user's profile folder</returns>
        [Beta]
        public static string GetUserPath() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}