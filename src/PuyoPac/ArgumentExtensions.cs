using PuyoPac.IO;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace PuyoPac
{
    static class ArgumentExtensions
    {
        public static Argument<T> ExistingOrSearchPatternsOnly<T>(this Argument<T> argument)
            where T : IEnumerable<FileSystemEnumerable>
        {
            argument.AddValidator(
                a => a.Tokens
                    .Select(t => t.Value)
                    .Where(path => !IsExistingOrSearchPattern(path))
                    .Select(path => string.Format(Properties.Resources.InvalidFilePathOrSearchPattern, path))
                    .FirstOrDefault());

            return argument;
        }

        private static bool IsExistingOrSearchPattern(string path)
        {
            // Check to see if this path exists.
            if (Directory.Exists(path) || File.Exists(path))
            {
                return true;
            }

            // Check if path is a search pattern.
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (directory == string.Empty)
                {
                    directory = Environment.CurrentDirectory;
                }
                Directory.EnumerateFileSystemEntries(directory, Path.GetFileName(path));
                return true;
            }
            catch
            {
            }

            return false;
        }
    }
}
