using PuyoPac.HedgeLib.Compression;
using PuyoPac.HedgeLib.Exceptions;
using PuyoPac.HedgeLib.IO;
using System;

namespace PuyoPac.HedgeLib.Headers
{
    public class PACx403Header : BINAHeader
    {
        // Variables/Constants
        public uint ID, DependencyEntriesLength, CompressedChunksLength, StringTableLength,
            RootOffset, RootCompressedLength, RootLength,
            Length;

        public bool HasDependencies;
        public bool HasCompressedBlocks;

        public PacCompressionFormat CompressionFormat = PacCompressionFormat.None;

        public const string PACxSignature = "PACx";
        public const uint LengthWithoutDependencies = 0x20, LengthWithDependencies = 0x30;
        public const ushort VersionNumber = 403, UnknownConstant = 0x108, DependencyConstant = 0x82, UnknownConstant1 = 0x208;

        // Constructors
        public PACx403Header(ushort version = VersionNumber, bool isBigEndian = false, bool hasDependencies = false)
        {
            IsBigEndian = isBigEndian;
            Version = version;
            Length = hasDependencies ? LengthWithDependencies : LengthWithoutDependencies;
        }

        public PACx403Header(ExtendedBinaryReader reader)
        {
            IsBigEndian = false;
            Read(reader);
        }

        // Methods
        public override void Read(ExtendedBinaryReader reader)
        {
            // PACx Header
            string sig = reader.ReadSignature(4);
            if (sig != PACxSignature)
                throw new InvalidSignatureException(PACxSignature, sig);

            // Version String
            string verString = reader.ReadSignature(3);
            if (!ushort.TryParse(verString, out Version))
            {
                throw new InvalidSignatureException(PACxSignature + Version, sig + verString);
            }

            reader.IsBigEndian = IsBigEndian =
                (reader.ReadChar() == BigEndianFlag);

            ID = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();

            RootOffset = reader.ReadUInt32();
            RootCompressedLength = reader.ReadUInt32();
            RootLength = reader.ReadUInt32();

            var flags = reader.ReadInt16();
            HasDependencies = (flags & 0x2) != 0;
            HasCompressedBlocks = (flags & 0x1) != 0;

            ushort uk1 = reader.ReadUInt16();
            /*if (uk1 != UnknownConstant)
            {
                Console.WriteLine($"WARNING: Unknown1 != 0x108! ({uk1})");
            }*/
            if (uk1 == UnknownConstant)
            {
                CompressionFormat = PacCompressionFormat.Deflate;
            }
            else if (uk1 == UnknownConstant1)
            {
                CompressionFormat = PacCompressionFormat.Lz4;
            }
            else
            {
                Console.WriteLine($"WARNING: Unknown1 doesn't match a known value! ({uk1})");
            }

            if ((flags & 0x80) != 0)
            {
                DependencyEntriesLength = reader.ReadUInt32();
                CompressedChunksLength = reader.ReadUInt32();
                StringTableLength = reader.ReadUInt32();
                FinalTableLength = reader.ReadUInt32();

                Length = LengthWithDependencies;
            }
            else
            {
                Length = LengthWithoutDependencies;
            }

            //Length = HasDependencies ? LengthWithDependencies : LengthWithoutDependencies;
            reader.Offset = Length;
        }

        public override void PrepareWrite(ExtendedBinaryWriter writer)
        {
            if (HasDependencies)
            {
                writer.WriteNulls(LengthWithDependencies);
            }
            else
            {
                writer.WriteNulls(Length);
            }


            writer.IsBigEndian = IsBigEndian;
        }

        public override void FinishWrite(ExtendedBinaryWriter writer)
        {
            writer.WriteSignature(PACxSignature);
            writer.WriteSignature(Version.ToString());
            writer.Write((IsBigEndian) ? BigEndianFlag : LittleEndianFlag);
            writer.Write(ID);
            writer.Write(FileSize);

            writer.Write(RootOffset);
            writer.Write(RootCompressedLength);
            writer.Write(RootLength);
            writer.Write(HasDependencies ? DependencyConstant : (ushort)0);
            writer.Write(UnknownConstant);

            if (HasDependencies || HasCompressedBlocks)
            {
                writer.Write(DependencyEntriesLength);
                writer.Write(CompressedChunksLength);
                writer.Write(StringTableLength);
                writer.Write(FinalTableLength);
            }
        }
    }
}