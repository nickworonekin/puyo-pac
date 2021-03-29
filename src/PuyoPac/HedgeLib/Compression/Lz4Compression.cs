using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuyoPac.HedgeLib.Compression
{
    internal static class Lz4Compression
    {
        private static int CompressBlock(Stream source, int sourceBlockLength, Stream destination)
        {
            var sourceBuffer = new byte[sourceBlockLength];
            var destinationBuffer = new byte[LZ4Codec.MaximumOutputSize(sourceBlockLength)];

            var numRead = 0;
            do
            {
                var n = source.Read(sourceBuffer, numRead, sourceBlockLength);
                if (n == 0)
                {
                    break;
                }

                numRead += n;
                sourceBlockLength -= n;
            }
            while (sourceBlockLength > 0);

            var destinationBlockLength = LZ4Codec.Encode(sourceBuffer, destinationBuffer, LZ4Level.L09_HC);

            destination.Write(destinationBuffer, 0, destinationBlockLength);

            return destinationBlockLength;
        }

        /// <summary>
        /// Compresses the data from <see cref="source"/> using LZ4 compression and writes the compressed data to <see cref="destination"/>.
        /// </summary>
        /// <param name="source">The stream containing the data to compress.</param>
        /// <param name="destination">The stream where the compressed data will be written to.</param>
        /// <returns></returns>
        /// <remarks>This method compresses data using the LZ4 compressed block format.</remarks>
        public static (long, List<Lz4CompressionBlock>) Compress(Stream source, Stream destination)
        {
            var compressedBlocks = new List<Lz4CompressionBlock>();

            long compressedLength = 0;
            var remainingLength = (uint)source.Length;

            while (remainingLength > 0)
            {
                var blockLength = Math.Min(remainingLength, Mem.K64);
                var compressedBlockLength = (uint)CompressBlock(source, (int)blockLength, destination);

                compressedBlocks.Add(new Lz4CompressionBlock
                {
                    CompressedLength = compressedBlockLength,
                    Length = blockLength,
                });

                remainingLength -= blockLength;
                compressedLength += compressedBlockLength;
            }

            return (compressedLength, compressedBlocks);
        }

        /// <summary>
        /// Compresses the data from <see cref="source"/> using LZ4 compression and returns the compressed data in a new stream.
        /// </summary>
        /// <param name="source">The stream containing the data to compress.</param>
        /// <returns></returns>
        /// <remarks>This method compresses data using the LZ4 compressed block format.</remarks>
        public static (MemoryStream, List<Lz4CompressionBlock>) Compress(Stream source)
        {
            var destination = new MemoryStream();
            var (_, compressedBlocks) = Compress(source, destination);
            destination.Seek(0, SeekOrigin.Begin);

            return (destination, compressedBlocks);
        }

        private static void DecompressBlock(Stream source, int sourceBlockLength, Stream destination, int destinationBlockLength)
        {
            var sourceBuffer = new byte[sourceBlockLength];
            var destinationBuffer = new byte[destinationBlockLength];

            var numRead = 0;
            do
            {
                var n = source.Read(sourceBuffer, numRead, sourceBlockLength);
                if (n == 0)
                {
                    break;
                }

                numRead += n;
                sourceBlockLength -= n;
            }
            while (sourceBlockLength > 0);

            var decompressedLength = LZ4Codec.Decode(sourceBuffer, destinationBuffer);

            if (decompressedLength != destinationBlockLength)
            {
                throw new InvalidDataException("Decompressed data size doesn't match expected size.");
            }

            destination.Write(destinationBuffer);
        }

        /// <summary>
        /// Decompresses the LZ4 compressed data from <see cref="source"/> and writes the decompressed data to <see cref="destination"/>.
        /// </summary>
        /// <param name="source">The stream containing the LZ4 compressed data to decompress.</param>
        /// <param name="compressedBlocks"></param>
        /// <param name="destination">The stream where the decompressed data will be written to.</param>
        /// <remarks>This method decompresses data in the LZ4 compressed block format.</remarks>
        public static void Decompress(Stream source, IEnumerable<Lz4CompressionBlock> compressedBlocks, Stream destination)
        {
            foreach (var compressedBlock in compressedBlocks)
            {
                DecompressBlock(source, (int)compressedBlock.CompressedLength, destination, (int)compressedBlock.Length);
            }
        }

        /// <summary>
        /// Decompresses the LZ4 compressed data from <see cref="source"/> and returns the decompressed data in a new stream.
        /// </summary>
        /// <param name="source">The stream containing the LZ4 compressed data to decompress.</param>
        /// <param name="compressedBlocks"></param>
        /// <returns></returns>
        /// <remarks>This method decompresses data in the LZ4 compressed block format.</remarks>
        public static MemoryStream Decompress(Stream source, IEnumerable<Lz4CompressionBlock> compressedBlocks)
        {
            var destination = new MemoryStream();
            Decompress(source, compressedBlocks, destination);
            destination.Seek(0, SeekOrigin.Begin);

            return destination;
        }
    }
}
