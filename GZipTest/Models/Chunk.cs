using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace GZipTest.Models
{
    public class Chunk
    {
        public int Number { get; set; }
        public int Size { get; set; }
        public byte[] Data { get; set; }

        public CompressedChunk Compress()
        {
            var compressed = new CompressedChunk
            {
                Number = Number,
                InitialSize = Size
            };

            using (var memory = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memory, CompressionMode.Compress))
                {
                    gzipStream.Write(Data, 0, Size);
                }

                compressed.Data = memory.ToArray();
                compressed.Size = compressed.Data.Length;
            }

            return compressed;
        }

        public bool IsCompressible
        {
            get
            {
                var compressed = Compress();
                return Size > compressed.Size;
            }
        }
    }
}
