using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PuyoPac.IO
{
    class FileSystemEnumerable : IEnumerable<string>
    {
        private IEnumerable<string> files;

        public FileSystemEnumerable(string path)
        {
            if (Directory.Exists(path))
            {
                // This is a directory, add all the files in it
                files = Directory.EnumerateFiles(path);
            }
            else
            {
                // This is a file (or has wildcards), add all the files that match it
                var directory = Path.GetDirectoryName(path);
                if (directory == string.Empty)
                {
                    directory = Environment.CurrentDirectory;
                }
                files = Directory.EnumerateFiles(directory, Path.GetFileName(path));
            }
        }

        public IEnumerator<string> GetEnumerator() => files.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
