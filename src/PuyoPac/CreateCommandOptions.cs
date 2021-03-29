using PuyoPac.IO;
using System.Collections.Generic;

namespace PuyoPac
{
    class CreateCommandOptions
    {
        public List<string> Dependency { get; set; }

        public string Compression { get; set; }

        public bool NoSplits { get; set; }

        public bool NoAutoDependencies { get; set; }

        public string Output { get; set; }

        public List<FileSystemEnumerable> Inputs { get; set; }
    }
}
