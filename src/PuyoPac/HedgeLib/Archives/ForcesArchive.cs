using PuyoPac.HedgeLib.IO;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using PuyoPac.HedgeLib.Headers;
using System.Text;
using System.IO.Compression;
using PuyoPac.IO;

namespace PuyoPac.HedgeLib.Archives
{
    // HUGELY based off of Skyth's SFPac/Forces PAC specification with permission (thanks bb)
    public class ForcesArchive : Archive
    {
        // Variables/Constants
        public PACx403Header ContainerHeader = new PACx403Header();
        public PACxHeader Header = new PACxHeader();
        public const string Extension = ".pac", Signature = "PACx";
        public const uint DefaultSplitSize = 0x1E00000, DefaultCompressSize = 0x19000;

        // Huge thanks to Skyth for these lists as well!
        protected readonly Dictionary<string, string> Types = new Dictionary<string, string>()
        {
            { ".asm", "ResAnimator" },
            { ".anm.hkx", "ResAnimSkeleton" },
            {".uv-anim", "ResAnimTexSrt"},
            {".material", "ResMirageMaterial"},
            {".model", "ResModel"},
            {".rfl", "ResReflection"},
            {".skl.hkx", "ResSkeleton"},
            {".dds", "ResTexture"},
            {".cemt", "ResCyanEffect"},
            {".cam-anim", "ResAnimCameraContainer"},
            {".effdb", "ResParticleLocation"},
            {".mat-anim", "ResAnimMaterial"},
            {".phy.hkx", "ResHavokMesh"},
            {".vis-anim", "ResAnimVis"},
            {".scfnt", "ResScalableFontSet"},
            {".pt-anim", "ResAnimTexPat"},
            {".scene", "ResScene"},
            {".pso", "ResMiragePixelShader"},
            {".vso", "ResMirageVertexShader"},
            {".shader-list", "ResShaderList"},
            {".vib", "ResVibration"},
            {".bfnt", "ResBitmapFont"},
            {".codetbl", "ResCodeTable"},
            {".cnvrs-text", "ResText"},
            {".cnvrs-meta", "ResTextMeta"},
            {".cnvrs-proj", "ResTextProject"},
            {".shlf", "ResSHLightField"},
            {".swif", "ResSurfRideProject"},
            {".gedit", "ResObjectWorld"},
            {".fxcol.bin", "ResFxColFile"},
            {".path", "ResSplinePath"},
            {".lit-anim", "ResAnimLightContainer"},
            {".gism", "ResGismoConfig"},
            {".light", "ResMirageLight"},
            {".probe", "ResProbe"},
            {".svcol.bin", "ResSvCol"},
            {".terrain-instanceinfo", "ResMirageTerrainInstanceInfo"},
            {".terrain-model", "ResMirageTerrainModel"},
            {".model-instanceinfo", "ResModelInstanceInfo"},
            {".grass.bin", "ResTerrainGrassInfo" },
            {".pss", "ResPss" }
        };

        protected readonly string[] RootExclusiveTypes = new string[]
        {
            ".asm",
            ".anm.hkx",
            ".cemt",
            ".phy.hkx",
            ".skl.hkx",
            ".rfl",
            ".bfnt",
            ".effdb",
            ".vib",
            ".scene",
            ".shlf",
            ".scfnt",
            ".codetbl",
            ".cnvrs-text",
            ".swif",
            ".fxcol.bin",
            ".path",
            ".gism",
            ".light",
            ".probe",
            ".svcol.bin",
            ".terrain-instanceinfo",
            ".model-instanceinfo",
            ".grass.bin",
            ".shader-list",
            ".gedit",
            ".cnvrs-meta",
            ".cnvrs-proj",
            ".pss",
        };

        protected enum DataEntryTypes : ulong
        {
            Regular,
            NotHere,
            BINAFile
        }

        // Constructors
        public ForcesArchive() : base() { }
        public ForcesArchive(Archive arc) : base(arc) { }

        // Methods
        public override void Load(string filePath)
        {
            using var fileStream = File.OpenRead(filePath);
            Load(fileStream);
        }

        public override void Load(Stream fileStream)
        {
            // Read PACx403 Header
            var reader = new BINAReader(fileStream);
            ContainerHeader.Read(reader);

            // Add the root PAC to the PAC entries, then loop through it and any PACs that may be added
            var pacEntries = new Queue<PacEntry>();
            pacEntries.Enqueue(new PacEntry(ContainerHeader.RootOffset, ContainerHeader.RootCompressedLength, ContainerHeader.RootLength));
            while (pacEntries.TryDequeue(out var entry))
            {
                LoadPac(entry.Open(fileStream), pacEntries);
            }
        }

        protected void LoadPac(Stream fileStream, Queue<PacEntry> pacEntries)
        {
            // Read PACx Header
            var reader = new BINAReader(fileStream);
            Header.Read(reader);

            // Read Node Trees and compute Data Entry count
            var typeTree = new NodeTree(reader);
            var fileTrees = new NodeTree[typeTree.DataNodeIndices.Count];

            int dataEntryCount = 0;
            Node dataNode;

            for (int i = 0; i < fileTrees.Length; ++i)
            {
                dataNode = typeTree.Nodes[typeTree.DataNodeIndices[i]];
                reader.JumpTo((long)dataNode.Data);
                fileTrees[i] = new NodeTree(reader);
                dataEntryCount += fileTrees[i].DataNodeIndices.Count;
            }

            // Read Data Entries
            var dataEntries = new DataEntry[dataEntryCount];
            int dataEntryIndex = -1;

            foreach (var fileTree in fileTrees)
            {
                for (int i = 0; i < fileTree.DataNodeIndices.Count; ++i)
                {
                    dataNode = fileTree.Nodes[fileTree.DataNodeIndices[i]];
                    reader.JumpTo((long)dataNode.Data);
                    dataEntries[++dataEntryIndex] = new DataEntry(reader);
                }
            }

            // Read splits
            if (Header.SplitCount != 0)
            {
                reader.JumpTo(Header.NodeTreeLength + 0x10, false);
                for (uint i = 0; i < Header.SplitCount; i++)
                {
                    reader.ReadInt64(); // We don't care about the name of the split archive
                    var compressedLength = reader.ReadUInt32();
                    var length = reader.ReadUInt32();
                    var offset = reader.ReadInt64();

                    pacEntries.Enqueue(new PacEntry(offset, compressedLength, length));
                }
            }

            // Read Data
            int dataSize, bytesRead;
            foreach (var dataEntry in dataEntries)
            {
                if (dataEntry.DataType == DataEntryTypes.NotHere)
                    continue;

                reader.JumpTo(dataEntry.DataOffset);
                dataSize = dataEntry.Data.Length;

                do
                {
                    bytesRead = reader.Read(dataEntry.Data, 0, dataSize);
                    if (bytesRead == 0)
                    {
                        throw new EndOfStreamException(
                            "Could not read file data from PAC.");
                    }

                    dataSize -= bytesRead;
                }
                while (dataSize > 0);
            }

            // Get file names and add files to archive
            var builder = new StringBuilder(byte.MaxValue);
            dataEntryIndex = -1;

            foreach (var fileTree in fileTrees)
            {
                for (int i = 0; i < fileTree.DataNodeIndices.Count; ++i)
                {
                    var dataEntry = dataEntries[++dataEntryIndex];
                    if (dataEntry.DataType == DataEntryTypes.NotHere)
                        continue;

                    dataNode = fileTree.Nodes[fileTree.DataNodeIndices[i]];

                    // Read extension
                    reader.JumpTo(dataEntry.ExtensionOffset);
                    builder.Append('.');
                    builder.Append(reader.ReadNullTerminatedString());

                    // Get full file name
                    string pth;
                    for (int i2 = dataNode.ParentIndex; i2 > 0;)
                    {
                        pth = fileTree.Nodes[i2].Name;
                        i2 = fileTree.Nodes[i2].ParentIndex;

                        if (string.IsNullOrEmpty(pth))
                            continue;

                        builder.Insert(0, pth);
                    }

                    // Add to archive
                    Data.Add(new ArchiveFile(
                        builder.ToString(),
                        dataEntry.Data));

                    builder.Clear();
                }
            }
        }

        public override void Save(string filePath, bool overwrite = false)
        {
            Save(filePath, overwrite: overwrite);
        }

        public void Save(string filePath,
            bool overwrite = false,
            IList<string> dependencies = null,
            uint? splitCount = DefaultSplitSize,
            uint? compressSize = DefaultCompressSize)
        {
            if (!overwrite && File.Exists(filePath))
                throw new Exception("Cannot save the given file - it already exists!");

            var fileInfo = new FileInfo(filePath);
            string shortName = fileInfo.Name.Substring(0,
                fileInfo.Name.IndexOf('.'));

            var hasDependencies = dependencies?.Any() == true;
            ContainerHeader.HasDependencies = hasDependencies;

            using var fileStream = File.Create(filePath);

            // BINA Header
            var writer = new BINAWriter(fileStream, ContainerHeader);
            if (ContainerHeader.ID == 0)
            {
                // Get a random non-zero value between 1 and 0xFFFFFFFF
                // because I care too much™
                var rand = new Random();
                do
                {
                    ContainerHeader.ID = unchecked((uint)rand.Next(
                        int.MinValue, int.MaxValue));
                }
                while (ContainerHeader.ID == 0);
            }

            // Write out dependencies
            if (hasDependencies)
            {
                writer.Write((long)dependencies.Count);
                writer.AddOffset("dependencyNodesOffset", 8);
                writer.FillInOffsetLong("dependencyNodesOffset", removeOffset: false);
                ContainerHeader.DependencyEntriesLength = 16;

                for (int i = 0; i < dependencies.Count; i++)
                {
                    writer.AddString($"dependencyName{i}", dependencies[i], 8);
                    ContainerHeader.DependencyEntriesLength += 8;
                }

                writer.WriteStringTable(ContainerHeader);
                writer.WriteFooter(ContainerHeader);
            }

            // Generate Split Archives
            if (splitCount.HasValue)
            {
                // Determine whether splitting is necessary
                bool doSplit = false;
                foreach (var data in Data)
                {
                    var file = (data as ArchiveFile);
                    if (file == null)
                        continue;

                    string ext = file.Name.Substring(file.Name.IndexOf('.'));
                    if (!RootExclusiveTypes.Contains(ext))
                    {
                        doSplit = true;
                        break;
                    }
                }

                // Save Split PACs if necessary
                List<PacEntry> pacList = null;
                if (doSplit)
                {
                    pacList = new List<PacEntry>();
                    int startIndex = 0, arcIndex = 0;
                    uint compressedLength, length;

                    while (startIndex != -1)
                    {
                        string fileName = $"{shortName}{Extension}.{arcIndex.ToString("000")}";
                        var startPosition = fileStream.Position;

                        (startIndex, compressedLength, length) = SavePac(fileStream, splitCount, compressSize, startIndex);
                        writer.FixPadding(16);
                        pacList.Add(new PacEntry(startPosition, compressedLength, length, fileName));
                        ++arcIndex;
                    }
                }

                // Save Root PAC
                ContainerHeader.RootOffset = (uint)fileStream.Position;
                (_, ContainerHeader.RootCompressedLength, ContainerHeader.RootLength) = SavePac(fileStream, null, compressSize, 0, pacList);
                writer.FixPadding(16);
            }

            // Generate archive without splits
            else
            {
                ContainerHeader.RootOffset = (uint)fileStream.Position;
                (_, ContainerHeader.RootCompressedLength, ContainerHeader.RootLength) = SavePac(fileStream, null, compressSize);
                writer.FixPadding(16);
            }

            // Fill-In Header
            uint fileSize = (uint)fileStream.Position;
            fileStream.Position = 0;

            ContainerHeader.FileSize = fileSize;
            ContainerHeader.FinishWrite(writer);

            fileStream.Position = fileSize;
        }

        public override void Save(Stream fileStream)
        {
            SavePac(fileStream, null, null);
        }

        protected (int, uint, uint) SavePac(Stream fileStream,
            uint? sizeLimit,
            uint? compressSize,
            int startIndex = 0,
            List<PacEntry> splitList = null)
        {
            uint size = 0;
            int endIndex = -1;
            bool isRootPAC = !sizeLimit.HasValue;

            using var memoryStream = new MemoryStream();

            // BINA Header
            var writer = new BINAWriter(memoryStream, Header);
            if (Header.ID == 0)
            {
                // Get a random non-zero value between 1 and 0xFFFFFFFF
                // because I care too much™
                var rand = new Random();
                do
                {
                    Header.ID = unchecked((uint)rand.Next(
                        int.MinValue, int.MaxValue));
                }
                while (Header.ID == 0);
            }

            // Generate list of files to pack, categorized by extension
            var filesByExt = new Dictionary<string, List<DataContainer>>();
            for (int i = startIndex; i < Data.Count; ++i)
            {
                var file = (Data[i] as ArchiveFile);
                if (file == null)
                    continue;

                int extIndex = file.Name.IndexOf('.');
                if (extIndex == -1)
                {
                    Console.WriteLine(
                        "WARNING: Skipped {0} as it has no extension!",
                        file.Name);
                    continue;
                }

                string ext = file.Name.Substring(extIndex);
                if (!Types.ContainsKey(ext))
                {
                    Console.WriteLine(
                        "WARNING: Skipped {0} as its extension ({1}) is unsupported!",
                        file.Name, ext);
                    continue;
                }

                // Root-Exclusive Type Check
                var dataEntry = new DataEntry(file.Data);
                if (isRootPAC)
                {
                    dataEntry.DataType = (!RootExclusiveTypes.Contains(ext)) ?
                        DataEntryTypes.NotHere : DataEntryTypes.Regular; ;
                }
                else if (RootExclusiveTypes.Contains(ext))
                    continue;

                // BINA Header Check
                if (file.Data != null && file.Data.Length > 3 &&
                    file.Data[0] == 0x42 && file.Data[1] == 0x49 &&
                    file.Data[2] == 0x4E && file.Data[3] == 0x41)
                {
                    dataEntry.DataType = DataEntryTypes.BINAFile;
                }

                // Split if you exceed the sizeLimit
                if (!isRootPAC && sizeLimit.HasValue)
                {
                    // Not very accurate but close enough™
                    size += (uint)file.Data.Length;
                    if (size >= sizeLimit.Value)
                    {
                        endIndex = i;
                        break;
                    }
                }

                // Add Node to list, making the list first if necessary
                List<DataContainer> files;
                if (!filesByExt.ContainsKey(ext))
                {
                    files = new List<DataContainer>();
                    filesByExt.Add(ext, files);
                }
                else
                {
                    files = filesByExt[ext];
                }

                // We use substring instead of Path.GetFileNameWithoutExtension
                // to support files with multiple extensions (e.g. *.grass.bin)
                string shortName = file.Name.Substring(0, extIndex);
                files.Add(new DataContainer(shortName, dataEntry));
            }

            // Pack file list into Node Trees and generate types list
            //var fileTrees = new Dictionary<string, NodeTree>();
            var types = new List<DataContainer>();
            foreach (var fileType in filesByExt)
            {
                var fileTree = new NodeTree(fileType.Value)
                {
                    CustomData = fileType.Key.Substring(1)
                };

                types.Add(new DataContainer(
                    Types[fileType.Key], fileTree));
            }

            // Pack types list into Node Tree
            var typeTree = new NodeTree(types);

            // Write types Node Tree
            const string typeTreePrefix = "typeTree";
            typeTree.Write(writer, typeTreePrefix);

            // Write file Node Trees and generate an array of trees in the
            // same order as they'll be in the file for easier writing
            var fileTrees = new NodeTree[typeTree.DataNodeIndices.Count];
            for (int i = 0; i < fileTrees.Length; ++i)
            {
                int dataNodeIndex = typeTree.DataNodeIndices[i];
                var tree = (NodeTree)typeTree.Nodes[dataNodeIndex].Data;

                writer.FillInOffsetLong(
                    $"{typeTreePrefix}nodeDataOffset{dataNodeIndex}",
                    true, false);

                fileTrees[i] = tree;
                tree.Write(writer, $"fileTree{i}");
            }

            // Write Data Node Indices
            typeTree.WriteDataIndices(writer, typeTreePrefix);
            for (int i = 0; i < fileTrees.Length; ++i)
            {
                fileTrees[i].WriteDataIndices(writer, $"fileTree{i}");
            }

            // Write Child Node Indices
            typeTree.WriteChildIndices(writer, typeTreePrefix);
            for (int i = 0; i < fileTrees.Length; ++i)
            {
                fileTrees[i].WriteChildIndices(writer, $"fileTree{i}");
            }

            // Write Split PACs section
            if (isRootPAC && splitList != null)
            {
                writer.Write((ulong)splitList.Count);
                writer.AddOffset($"splitPACsOffset", 8);
                writer.FillInOffsetLong($"splitPACsOffset", true, false);
                Header.SplitListLength += 16;

                for (int i = 0; i < splitList.Count; ++i)
                {
                    writer.AddString($"splitPACName{i}", splitList[i].Name, 8);
                    Header.SplitListLength += 8;

                    writer.Write(splitList[i].CompressedLength);
                    writer.Write(splitList[i].Length);
                    writer.Write(splitList[i].Offset);
                    Header.SplitListLength += 16;
                }
            }

            // Write File Entries
            long fileEntriesOffset = memoryStream.Position;
            for (int i = 0; i < fileTrees.Length; ++i)
            {
                fileTrees[i].WriteDataEntries(writer, $"fileTree{i}",
                    (string)fileTrees[i].CustomData, Header.ID);
            }

            // Write String Table
            uint stringTablePos = (uint)memoryStream.Position;
            writer.WriteStringTable(Header);
            writer.FixPadding(8);

            // Write File Data
            long fileDataOffset = memoryStream.Position;
            for (int i = 0; i < fileTrees.Length; ++i)
            {
                fileTrees[i].WriteData(writer, $"fileTree{i}",
                    (string)fileTrees[i].CustomData);
            }

            // Write Offset Table
            writer.FixPadding(8);
            uint footerPos = writer.WriteFooter(Header);

            // Fill-In Header
            uint fileSize = (uint)memoryStream.Position;
            writer.BaseStream.Position = 0;

            Header.NodeTreeLength = (uint)(fileEntriesOffset -
                PACxHeader.Length) - Header.SplitListLength;

            Header.FileEntriesLength = stringTablePos - (uint)fileEntriesOffset;
            Header.StringTableLength = (uint)fileDataOffset - stringTablePos;
            Header.DataLength = (footerPos - (uint)fileDataOffset);

            // 5 if there are splits and this is the root, 2 if this is a split, 1 if no splits
            Header.PacType = (sizeLimit.HasValue) ? PACxHeader.PACTypes.IsSplit :
                (splitList != null) ? PACxHeader.PACTypes.HasSplits :
                PACxHeader.PACTypes.HasNoSplits;

            Header.SplitCount = (splitList == null) ? 0U : (uint)splitList.Count;
            Header.FinishWrite(writer);

            // Write the data to the main file stream and see if it should be compressed.
            uint compressedLength, length = (uint)memoryStream.Length;
            memoryStream.Position = 0;
            if (compressSize.HasValue && memoryStream.Length >= compressSize.Value)
            {
                var startPosition = fileStream.Position;
                using (var compressionStream = new DeflateStream(fileStream, CompressionLevel.Optimal, true))
                {
                    memoryStream.CopyTo(compressionStream);
                }
                var endPosition = fileStream.Position;

                compressedLength = (uint)(endPosition - startPosition);
            }
            else
            {
                memoryStream.CopyTo(fileStream);

                compressedLength = length;
            }

            return (endIndex, compressedLength, length);
        }

        // Other
        protected class DataContainer : IComparable
        {
            // Variables/Constants
            public string Name;
            public object Data;

            // Constructors
            public DataContainer() { }
            public DataContainer(string name, object data)
            {
                Name = name;
                Data = data;
            }

            // Methods
            public virtual int CompareTo(object obj)
            {
                if (obj == null)
                    return 1;

                if (obj is DataContainer data)
                    return Name.CompareTo(data.Name);

                throw new NotImplementedException(
                    $"Cannot compare {GetType()} to {obj.GetType()}!");
            }
        }

        protected class NodeTree
        {
            // Variables/Constants
            public List<Node> Nodes = new List<Node>();
            public List<int> DataNodeIndices = new List<int>();
            public Node RootNode => Nodes[0];
            public object CustomData;

            // Constructors
            public NodeTree()
            {
                Nodes.Add(new Node());
            }

            public NodeTree(List<DataContainer> dataList) : this()
            {
                Pack(RootNode, dataList);
            }

            public NodeTree(BINAReader reader)
            {
                Read(reader);
            }

            // Methods
            public Node AddChild(Node rootNode, string name,
                object data = null, bool putDataInSubNode = true)
            {
                if (putDataInSubNode && data != null)
                {
                    var childNode = rootNode.MakeChild(this, name, null);
                    return childNode.MakeChild(this, null, data); // no stop
                }

                return rootNode.MakeChild(this, name, data);
            }

            // Taken with permission from Skyth's SFPac
            public void Pack(Node rootNode, List<DataContainer> dataList)
            {
                int dataListLength = dataList.Count;
                if (dataListLength == 0)
                    return;

                if (dataListLength == 1)
                {
                    AddChild(rootNode, dataList[0].Name, dataList[0].Data);
                    return;
                }

                bool canPack = false;
                foreach (var data in dataList)
                {
                    if (string.IsNullOrEmpty(data.Name))
                    {
                        throw new InvalidDataException(
                            "Empty string found during node packing!");
                    }

                    foreach (var dataToCompare in dataList)
                    {
                        if (data != dataToCompare &&
                            data.Name[0] == dataToCompare.Name[0])
                        {
                            canPack = true;
                            break;
                        }
                    }

                    if (canPack)
                        break;
                }

                if (canPack)
                {
                    dataList.Sort();

                    string nameToCompare = dataList[0].Name;
                    int minLength = nameToCompare.Length;

                    var matches = new List<DataContainer>();
                    var noMatches = new List<DataContainer>();

                    foreach (var data in dataList)
                    {
                        int compareLength = System.Math.Min(minLength, data.Name.Length);
                        int matchLength = 0;

                        for (int i = 0; i < compareLength; ++i)
                        {
                            if (data.Name[i] != nameToCompare[i])
                                break;
                            else
                                ++matchLength;
                        }

                        if (matchLength >= 1)
                        {
                            matches.Add(data);
                            minLength = System.Math.Min(minLength, matchLength);
                        }
                        else
                        {
                            noMatches.Add(data);
                        }
                    }

                    if (matches.Count >= 1)
                    {
                        Node parentNode;
                        if (minLength == nameToCompare.Length)
                        {
                            parentNode = AddChild(rootNode,
                                dataList[0].Name, dataList[0].Data);

                            parentNode = Nodes[parentNode.ParentIndex];
                            matches.RemoveAt(0);
                        }
                        else
                        {
                            parentNode = AddChild(rootNode,
                                nameToCompare.Substring(0, minLength));
                        }

                        foreach (var x in matches)
                        {
                            x.Name = x.Name.Substring(minLength);
                        }

                        Pack(parentNode, matches);
                    }

                    if (noMatches.Count >= 1)
                    {
                        Pack(rootNode, noMatches);
                    }
                }
                else
                {
                    foreach (var data in dataList)
                    {
                        AddChild(rootNode, data.Name, data.Data);
                    }
                }
            }

            public void Read(BINAReader reader)
            {
                uint nodesCount = reader.ReadUInt32();
                uint dataNodeCount = reader.ReadUInt32();
                long nodesOffset = reader.ReadInt64();
                long dataNodeIndicesOffset = reader.ReadInt64();

                // Nodes
                reader.JumpTo(nodesOffset);
                Nodes.Capacity = (int)nodesCount;

                for (int i = 0; i < nodesCount; ++i)
                {
                    Nodes.Add(new Node(reader, false));
                }

                // Data Node Indices
                reader.JumpTo(dataNodeIndicesOffset);
                DataNodeIndices.Capacity = (int)dataNodeCount;

                for (int i = 0; i < dataNodeCount; ++i)
                {
                    DataNodeIndices.Add(reader.ReadInt32());
                }
            }

            public void Write(BINAWriter writer, string prefix)
            {
                writer.Write(Nodes.Count);
                writer.Write(DataNodeIndices.Count);
                writer.AddOffset($"{prefix}nodesOffset", 8);
                writer.AddOffset($"{prefix}dataNodeIndicesOffset", 8);

                // Nodes
                writer.FillInOffsetLong($"{prefix}nodesOffset", true, false);
                foreach (var node in Nodes)
                {
                    node.Write(writer, this, prefix);
                }
            }

            public void WriteDataIndices(BINAWriter writer, string prefix)
            {
                writer.FillInOffsetLong($"{prefix}dataNodeIndicesOffset", true, false);
                for (int i = 0; i < DataNodeIndices.Count; ++i)
                {
                    writer.Write(DataNodeIndices[i]);
                }

                writer.FixPadding(8);
            }

            public void WriteChildIndices(BINAWriter writer, string prefix)
            {
                foreach (var node in Nodes)
                {
                    node.WriteChildIndices(writer, prefix);
                }
            }

            public void WriteDataEntries(BINAWriter writer,
                string prefix, string extension, uint id)
            {
                foreach (var node in Nodes)
                {
                    node.WriteDataEntries(writer, prefix, extension, id);
                }
            }

            public void WriteData(BINAWriter writer,
                string prefix, string extension)
            {
                foreach (var node in Nodes)
                {
                    node.WriteData(writer, extension);
                }
            }
        }

        protected class Node
        {
            // Variables/Constants
            public List<int> ChildIndices = new List<int>();
            public object Data = null;
            public string Name = null;
            public int ParentIndex = -1, Index = 0, DataIndex = -1;

            public int ChildCount => ChildIndices.Count;

            // Constructors
            public Node() { }
            public Node(BINAReader reader, bool readChildIndices = true)
            {
                Read(reader, readChildIndices);
            }

            public Node(string name, int index,
                object data = null, int parentIndex = -1)
            {
                Name = name;
                Data = data;
                Index = index;
                ParentIndex = parentIndex;
            }

            // Methods
            /// <summary>
            /// ( ͡° ͜ʖ ͡°)
            /// </summary>
            public Node MakeChild(NodeTree tree, string name, object data = null)
            {
                var childNode = new Node(name,
                    tree.Nodes.Count, data, Index);

                if (data != null)
                {
                    childNode.DataIndex = tree.DataNodeIndices.Count;
                    tree.DataNodeIndices.Add(childNode.Index);
                }

                ChildIndices.Add(childNode.Index);
                tree.Nodes.Add(childNode);

                return childNode;
            }

            public int GetRecursiveChildCount(List<Node> nodes)
            {
                int count = ChildIndices.Count;
                for (int i = 0; i < count; ++i)
                {
                    count += nodes[ChildIndices[i]].
                        GetRecursiveChildCount(nodes);
                }

                return count;
            }

            public void Read(BINAReader reader, bool readChildIndices = true)
            {
                long nameOffset = reader.ReadInt64();
                long dataOffset = reader.ReadInt64();
                long childIndexTableOffset = reader.ReadInt64();

                ParentIndex = reader.ReadInt32();
                Index = reader.ReadInt32();
                DataIndex = reader.ReadInt32();

                ushort childCount = reader.ReadUInt16();
                bool hasData = reader.ReadBoolean();
                byte fullPathSize = reader.ReadByte(); // Not counting this node in

                // Read name
                if (nameOffset != 0)
                    Name = reader.GetString((uint)nameOffset);

                // (MINOR HACK) Store data offset in Data
                // to be read later, avoiding some Seeks
                if (hasData && dataOffset != 0)
                    Data = dataOffset;

                // Read Child Indices
                if (!readChildIndices)
                    return;

                long curPos = reader.BaseStream.Position;
                reader.JumpTo(childIndexTableOffset);
                ChildIndices.Capacity = childCount;

                for (int i = 0; i < childCount; ++i)
                {
                    ChildIndices.Add(reader.ReadInt32());
                }

                reader.JumpTo(curPos);
            }

            public void Write(BINAWriter writer, NodeTree tree, string prefix)
            {
                // Name
                if (!string.IsNullOrEmpty(Name))
                    writer.AddString($"{prefix}nodeNameOffset{Index}", Name, 8);
                else
                    writer.Write(0UL);

                // Data
                bool hasData = (Data != null);
                if (hasData)
                    writer.AddOffset($"{prefix}nodeDataOffset{Index}", 8);
                else
                    writer.Write(0UL);

                // Child Index Table Offset
                if (ChildIndices.Count > 0)
                    writer.AddOffset($"{prefix}nodeChildIDTableOffset{Index}", 8);
                else
                    writer.Write(0UL);

                // Indices
                writer.Write(ParentIndex);
                writer.Write(Index);
                writer.Write(DataIndex);

                // Other
                writer.Write((ushort)ChildIndices.Count);
                writer.Write(hasData);

                // Full Path Size
                string pth;
                int fullPathSize = 0;

                for (int i = ParentIndex; i > 0;)
                {
                    pth = tree.Nodes[i].Name;
                    i = tree.Nodes[i].ParentIndex;

                    if (string.IsNullOrEmpty(pth))
                        continue;

                    fullPathSize += pth.Length;
                    if (fullPathSize > byte.MaxValue)
                    {
                        throw new NotSupportedException(
                            "Forces cannot have file names > 255 characters in length!");
                    }
                }

                writer.Write((byte)fullPathSize);
            }

            public void WriteChildIndices(BINAWriter writer, string prefix)
            {
                if (ChildIndices.Count < 1)
                    return;

                writer.FillInOffsetLong(
                    $"{prefix}nodeChildIDTableOffset{Index}",
                    true, false);

                for (int i = 0; i < ChildIndices.Count; ++i)
                {
                    writer.Write(ChildIndices[i]);
                }

                writer.FixPadding(8);
            }

            public void WriteDataEntries(BINAWriter writer,
                string prefix, string extension, uint id)
            {
                if (Data != null && Data is DataEntry dataEntry)
                {
                    writer.FillInOffsetLong(
                        $"{prefix}nodeDataOffset{Index}", true, false);

                    dataEntry.Write(writer, extension, id, DataIndex);
                }
            }

            public void WriteData(BINAWriter writer, string extension)
            {
                // Write File Data
                if (Data != null && Data is DataEntry dataEntry)
                {
                    dataEntry.WriteData(writer, extension, DataIndex);
                }
            }
        }

        protected class DataEntry
        {
            // Variables/Constants
            public byte[] Data;
            public long DataOffset, ExtensionOffset;
            public DataEntryTypes DataType = DataEntryTypes.Regular;

            // Constructors
            public DataEntry() { }
            public DataEntry(BINAReader reader)
            {
                Read(reader);
            }

            public DataEntry(byte[] data)
            {
                Data = data;
            }

            // Methods
            public void Read(BINAReader reader)
            {
                uint id = reader.ReadUInt32();
                long dataSize = reader.ReadInt64();
                reader.JumpAhead(4);

                DataOffset = reader.ReadInt64();
                reader.JumpAhead(8);

                ExtensionOffset = reader.ReadInt64();
                DataType = (DataEntryTypes)reader.ReadUInt64();

                if (DataType != DataEntryTypes.NotHere)
                    Data = new byte[dataSize];
            }

            public void Write(BINAWriter writer, string extension, uint id, int index)
            {
                writer.Write(id);
                writer.Write((ulong)Data.Length);
                writer.Write(0U);

                if (DataType == DataEntryTypes.NotHere)
                    writer.Write(0UL);
                else
                    writer.AddOffset($"{extension}fileDataOffset{index}", 8);

                writer.Write(0UL);
                writer.AddString($"{extension}extDataOffset{index}", extension, 8);
                writer.Write((ulong)DataType);
            }

            public void WriteData(BINAWriter writer, string extension, int index)
            {
                if (DataType == DataEntryTypes.NotHere)
                    return;

                writer.FixPadding(16);
                writer.FillInOffsetLong(
                    $"{extension}fileDataOffset{index}", true, false);

                writer.Write(Data);
            }
        }

        protected class PacEntry
        {
            public long Offset { get; }
            public uint CompressedLength { get; }
            public uint Length { get; }
            public string Name { get; }

            public PacEntry(long offset, uint compressedLength, uint length)
                : this(offset, compressedLength, length, null)
            {
            }

            public PacEntry(long offset, uint compressedLength, uint length, string name)
            {
                Offset = offset;
                CompressedLength = compressedLength;
                Length = length;
                Name = name;
            }

            public Stream Open(Stream stream)
            {
                if (CompressedLength != Length)
                {
                    using var compressedStream = new DeflateStream(new StreamView(stream, Offset, CompressedLength), CompressionMode.Decompress);
                    var decompressedStream = new MemoryStream();
                    compressedStream.CopyTo(decompressedStream);
                    decompressedStream.Position = 0;

                    return decompressedStream;
                }

                return new StreamView(stream, Offset, Length);
            }
        }
    }
}