using K4os.Compression.LZ4;
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
        public static int Compress(Stream source, int sourceBlockLength, Stream destination)
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

            var destinationBlockLength = LZ4Codec.Encode(sourceBuffer, destinationBuffer);

            destination.Write(destinationBuffer);

            return destinationBlockLength;
        }

        public static void Decompress(Stream source, int sourceBlockLength, Stream destination, int destinationBlockLength)
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

            LZ4Codec.Decode(sourceBuffer, destinationBuffer);

            destination.Write(destinationBuffer);
        }
    }
}
