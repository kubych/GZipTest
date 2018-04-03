using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace GZipTest.Models
{
    public class CompressedChunk
    {
        public int Number { get; set; }
        public int InitialSize { get; set; }
        public int Size { get; set; }
        public byte[] Data { get; set; }

        public Chunk Decompress()
        {
            var chunk = new Chunk
            {
                Number = Number,
                Size = InitialSize,
                Data = new byte[InitialSize]
            };

            using (var gzipStream = new GZipStream(new MemoryStream(Data), CompressionMode.Decompress))
                gzipStream.Read(chunk.Data, 0, chunk.Size);

            return chunk;
        }
    }
}
