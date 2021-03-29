using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuyoPac.HedgeLib.Compression
{
    /// <summary>
    /// Represents the data lengths of a compressed block of LZ4 compressed data.
    /// </summary>
    public struct Lz4CompressionBlock
    {
        /// <summary>
        /// The compressed length for this block of LZ4 compressed data.
        /// </summary>
        public uint CompressedLength;

        /// <summary>
        /// The uncompressed length for this block of LZ4 decompressed data.
        /// </summary>
        public uint Length;
    }
}
