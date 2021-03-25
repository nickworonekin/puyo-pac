using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuyoPac.HedgeLib.Compression
{
    internal static class DeflateCompression
    {
        /// <summary>
        /// Reads the data from <see cref="source"/> and writes the compressed data to <see cref="destination"/>.
        /// </summary>
        /// <param name="source">The stream containing the data to compress.</param>
        /// <param name="destination">The stream the compressed data will be written to.</param>
        /// <returns>The length of the compressed data.</returns>
        public static long Compress(Stream source, Stream destination)
        {
            var startPosition = destination.Position;
            using (var compressionStream = new DeflateStream(destination, CompressionLevel.Optimal, true))
            {
                source.CopyTo(compressionStream);
            }
            var endPosition = destination.Position;

            return endPosition - startPosition;
        }

        public static MemoryStream Compress(Stream source)
        {
            var destination = new MemoryStream();
            Compress(source, destination);
            destination.Seek(0, SeekOrigin.Begin);

            return destination;
        }

        public static void Decompress(Stream source, Stream destination)
        {
            using var compressionStream = new DeflateStream(source, CompressionMode.Decompress, true);

            compressionStream.CopyTo(destination);
        }

        public static MemoryStream Decompress(Stream source)
        {
            var destination = new MemoryStream();
            Decompress(source, destination);
            destination.Seek(0, SeekOrigin.Begin);

            return destination;
        }
    }
}
