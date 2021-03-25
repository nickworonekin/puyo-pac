using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuyoPac.HedgeLib.Compression
{
    public enum PacCompressionFormat
    {
        /// <summary>
        /// PAC does not compress its embedded PACs.
        /// </summary>
        None,

        /// <summary>
        /// PAC compresses its embedded PACs using DEFLATE compression.
        /// </summary>
        Deflate,

        /// <summary>
        /// PAC compresses its embedded PACs using LZ4 compression.
        /// </summary>
        Lz4,
    }
}
