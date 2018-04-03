using GZipTest.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest
{
    public class GZip
    {
        private readonly int chunkSize;
        private readonly int numberOfThreads;

        private ThreadSafeQueue<Chunk> chunksQueue;
        private ThreadSafeQueue<CompressedChunk> compressedChunksQueue;

        private int numberOfChunks;
        private int totalNumberOfChunks;
        private EventWaitHandle waitHandler;

        public GZip()
        {
            numberOfThreads = Environment.ProcessorCount;
            if (!int.TryParse(ConfigurationManager.AppSettings["chunkSize"], out chunkSize))
                chunkSize = 1024 * 64;

            chunksQueue = new ThreadSafeQueue<Chunk>(this.numberOfThreads);
            compressedChunksQueue = new ThreadSafeQueue<CompressedChunk>(this.numberOfThreads);
        }

        public void Compress(string input, string output)
        {
            var fileInfo = new FileInfo(input);
            numberOfChunks = (int)Math.Ceiling((decimal)fileInfo.Length / chunkSize);
            totalNumberOfChunks = numberOfChunks;
            waitHandler = new ManualResetEvent(false);

            for (var i = 0; i < numberOfThreads; i++)
            {
                var thread = new Thread(() =>
                {
                    while (numberOfChunks > 0)
                    {
                        var chunk = chunksQueue.Dequeue();
                        var compressed = chunk.Compress();
                        compressedChunksQueue.Enqueue(compressed);
                    }
                })
                {
                    IsBackground = true
                };
                thread.Start();
            }

            var writerThread = new Thread(() =>
            {
                using (var fileStream = new FileStream(output, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(BitConverter.GetBytes(numberOfChunks), 0, sizeof(int));
                    while (numberOfChunks > 0)
                    {
                        var compressed = compressedChunksQueue.Dequeue();

                        fileStream.Write(BitConverter.GetBytes(compressed.Number), 0, sizeof(int));
                        fileStream.Write(BitConverter.GetBytes(compressed.InitialSize), 0, sizeof(int));
                        fileStream.Write(BitConverter.GetBytes(compressed.Size), 0, sizeof(int));
                        fileStream.Write(compressed.Data, 0, compressed.Size);

                        ShowProgress();

                        if (Interlocked.Decrement(ref numberOfChunks) == 0)
                            waitHandler.Set();
                    }
                }
            })
            {
                IsBackground = true
            };
            writerThread.Start();

            using (var fileStream = new FileStream(input, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[chunkSize];
                var count = 0;
                var bytesRead = fileStream.Read(buffer, 0, chunkSize);
                while (bytesRead > 0)
                {
                    var chunk = new Chunk
                    {
                        Number = count,
                        Size = bytesRead,
                        Data = buffer
                    };
                    chunksQueue.Enqueue(chunk);
                    count++;
                    buffer = new byte[chunkSize];
                    bytesRead = fileStream.Read(buffer, 0, chunkSize);
                }
            }

            waitHandler.WaitOne();
            Console.WriteLine($"\rDone.");
        }

        public void Decompress(string input, string output)
        {
            using (var fileStream = new FileStream(input, FileMode.Open, FileAccess.Read))
            {
                var size = sizeof(int);
                var buffer = new byte[size];
                var bytesRead = fileStream.Read(buffer, 0, size);
                if (bytesRead == 0)
                    return;

                numberOfChunks = BitConverter.ToInt32(buffer, 0);
                totalNumberOfChunks = numberOfChunks;
                waitHandler = new ManualResetEvent(false);

                for (var i = 0; i < numberOfThreads; i++)
                {
                    var thread = new Thread(() =>
                    {
                        while (numberOfChunks > 0)
                        {
                            var compressed = compressedChunksQueue.Dequeue();
                            var decompressed = compressed.Decompress();
                            chunksQueue.Enqueue(decompressed);
                        }
                    })
                    {
                        IsBackground = true
                    };
                    thread.Start();
                }

                var writerThread = new Thread(() =>
                {
                    using (var writer = new FileStream(output, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        while (numberOfChunks > 0)
                        {
                            var chunk = chunksQueue.Dequeue();

                            writer.Seek(chunk.Number * chunkSize, SeekOrigin.Begin);
                            writer.Write(chunk.Data, 0, chunk.Size);

                            ShowProgress();

                            if (Interlocked.Decrement(ref numberOfChunks) == 0)
                                waitHandler.Set();
                        }
                    }
                })
                {
                    IsBackground = true
                };
                writerThread.Start();

                var headerBuffer = new byte[3 * size];
                bytesRead = fileStream.Read(headerBuffer, 0, 3 * size);

                while (bytesRead > 0)
                {
                    var compressed = new CompressedChunk
                    {
                        Number = BitConverter.ToInt32(headerBuffer, 0),
                        InitialSize = BitConverter.ToInt32(headerBuffer, size),
                        Size = BitConverter.ToInt32(headerBuffer, 2 * size)
                    };

                    compressed.Data = new byte[compressed.Size];
                    bytesRead = fileStream.Read(compressed.Data, 0, compressed.Size);
                    if (bytesRead == 0)
                        return;

                    compressedChunksQueue.Enqueue(compressed);
                    headerBuffer = new byte[3 * size];
                    bytesRead = fileStream.Read(headerBuffer, 0, 3 * size);
                }
            }

            waitHandler.WaitOne();
            Console.WriteLine($"\rDone.");
        }

        private void ShowProgress()
        {
            var progress = (totalNumberOfChunks - numberOfChunks) * 100 / totalNumberOfChunks;
            Console.Write($"\r{progress}%");
        }
    }
}
