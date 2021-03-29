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

        public const string PACxSignature = "PACx";
        public const uint LengthWithoutFlags = 0x20, LengthWithFlags = 0x30;
        public const ushort VersionNumber = 403, UnknownConstant = 0x108, UnknownConstant1 = 0x208;

        // Constructors
        public PACx403Header(ushort version = VersionNumber, bool isBigEndian = false, bool hasDependencies = false, bool hasCompressedBlocks = false)
        {
            IsBigEndian = isBigEndian;
            Version = version;
            Length = hasDependencies || hasCompressedBlocks ? LengthWithFlags : LengthWithoutFlags;
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
            if (!HasCompressedBlocks && uk1 != UnknownConstant)
            {
                Console.WriteLine($"WARNING: Unknown1 != 0x108! ({uk1})");
            }
            else if (HasCompressedBlocks && uk1 != UnknownConstant1)
            {
                Console.WriteLine($"WARNING: Unknown1 != 0x208! ({uk1})");
            }

            if ((flags & 0x80) != 0)
            {
                DependencyEntriesLength = reader.ReadUInt32();
                CompressedChunksLength = reader.ReadUInt32();
                StringTableLength = reader.ReadUInt32();
                FinalTableLength = reader.ReadUInt32();

                Length = LengthWithFlags;
            }
            else
            {
                Length = LengthWithoutFlags;
            }

            reader.Offset = Length;
        }

        public override void PrepareWrite(ExtendedBinaryWriter writer)
        {
            if (HasDependencies || HasCompressedBlocks)
            {
                writer.WriteNulls(LengthWithFlags);
            }
            else
            {
                writer.WriteNulls(LengthWithoutFlags);
            }


            writer.IsBigEndian = IsBigEndian;
        }

        public override void FinishWrite(ExtendedBinaryWriter writer)
        {
            ushort flags = 0;
            if (HasDependencies)
            {
                flags |= 0x82;
            }
            if (HasCompressedBlocks)
            {
                flags |= 0x81;
            }

            writer.WriteSignature(PACxSignature);
            writer.WriteSignature(Version.ToString());
            writer.Write((IsBigEndian) ? BigEndianFlag : LittleEndianFlag);
            writer.Write(ID);
            writer.Write(FileSize);

            writer.Write(RootOffset);
            writer.Write(RootCompressedLength);
            writer.Write(RootLength);
            writer.Write(flags);
            writer.Write(HasCompressedBlocks ? UnknownConstant1 : UnknownConstant);

            if ((flags & 0x80) != 0)
            {
                writer.Write(DependencyEntriesLength);
                writer.Write(CompressedChunksLength);
                writer.Write(StringTableLength);
                writer.Write(FinalTableLength);
            }
        }
    }
}