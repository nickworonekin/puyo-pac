using PuyoPac.HedgeLib.Exceptions;
using PuyoPac.HedgeLib.IO;
using System;

namespace PuyoPac.HedgeLib.Headers
{
    public class PACx403Header : BINAHeader
    {
        // Variables/Constants
        public uint ID, DependencyEntriesLength, StringTableLength,
            RootOffset, RootCompressedLength, RootLength,
            Length;

        public bool HasDependencies;

        public const string PACxSignature = "PACx";
        public const uint LengthWithoutDependencies = 0x20, LengthWithDependencies = 0x30;
        public const ushort VersionNumber = 403, UnknownConstant = 0x108, DependencyConstant = 0x82;

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
            HasDependencies = reader.ReadInt16() == DependencyConstant;

            ushort uk1 = reader.ReadUInt16();
            if (uk1 != UnknownConstant)
            {
                Console.WriteLine($"WARNING: Unknown1 != 0x108! ({uk1})");
            }

            Length = HasDependencies ? LengthWithDependencies : LengthWithoutDependencies;
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

            if (HasDependencies)
            {
                writer.Write(DependencyEntriesLength);
                writer.Write(0);
                writer.Write(StringTableLength);
                writer.Write(FinalTableLength);
            }
        }
    }
}