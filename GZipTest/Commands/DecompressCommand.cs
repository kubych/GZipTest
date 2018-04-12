using GZipTest.CommandDatas;
using GZipTest.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest.Commands
{
    public class DecompressCommand : ICommand<DecompressCommandData>
    {
        private int chunkSize;
        private readonly int numberOfThreads;

        private ThreadSafeQueue<Chunk> chunksQueue;
        private ThreadSafeQueue<CompressedChunk> compressedChunksQueue;
        private List<Thread> threads;

        private int numberOfChunks;
        private int totalNumberOfChunks;
        private EventWaitHandle waitHandler;

        public DecompressCommand()
        {
            numberOfThreads = Environment.ProcessorCount;
            chunksQueue = new ThreadSafeQueue<Chunk>(this.numberOfThreads);
            compressedChunksQueue = new ThreadSafeQueue<CompressedChunk>(this.numberOfThreads);
            threads = new List<Thread>();
        }

        public void Execute(DecompressCommandData commandData)
        {
            if (!File.Exists(commandData.InputFileName))
                throw new InvalidOperationException($"File '{commandData.InputFileName}' doesn't exists");
            using (var fileStream = new FileStream(commandData.InputFileName, FileMode.Open, FileAccess.Read))
            {
                var size = sizeof(int);
                var buffer = new byte[size];
                var bytesRead = fileStream.Read(buffer, 0, size);
                if (bytesRead == 0)
                    return;
                numberOfChunks = BitConverter.ToInt32(buffer, 0);
                totalNumberOfChunks = numberOfChunks;

                bytesRead = fileStream.Read(buffer, 0, size);
                if (bytesRead == 0)
                    return;
                chunkSize = BitConverter.ToInt32(buffer, 0);

                waitHandler = new ManualResetEvent(false);


                var headerBuffer = new byte[3 * size];
                bytesRead = fileStream.Read(headerBuffer, 0, 3 * size);
                if (bytesRead > 0)
                {
                    CreateDecompressionThreads(commandData);
                    CreateWriterThread(commandData);
                    StartThreads();
                }

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

        private void CreateDecompressionThreads(DecompressCommandData commandData)
        {
            for (var i = 0; i < numberOfThreads; i++)
            {
                threads.Add(new Thread((mainThread) =>
                {
                    try
                    {
                        while (numberOfChunks > 0)
                        {
                            var compressed = compressedChunksQueue.Dequeue();
                            var decompressed = compressed.Decompress();
                            chunksQueue.Enqueue(decompressed);
                        }
                    }
                    catch (Exception e)
                    {
                        commandData.Error = "Error during decompression of a chunk of data in thread";
                        commandData.Exception = e;
                        ((Thread)mainThread).Interrupt();
                    }
                })
                {
                    IsBackground = true
                });
            }
        }

        private void CreateWriterThread(DecompressCommandData commandData)
        {
            threads.Add(new Thread((mainThread) =>
            {
                try
                {
                    using (var writer = new FileStream(commandData.OutputFileName, FileMode.Create, FileAccess.Write))
                    {
                        while (numberOfChunks > 0)
                        {
                            var chunk = chunksQueue.Dequeue();

                            writer.Seek((long)chunk.Number * chunkSize, SeekOrigin.Begin);
                            writer.Write(chunk.Data, 0, chunk.Size);

                            ShowProgress();

                            if (Interlocked.Decrement(ref numberOfChunks) == 0)
                                waitHandler.Set();
                        }
                    }
                }
                catch (Exception e)
                {
                    commandData.Error = "Error during writing to output file";
                    commandData.Exception = e;
                    ((Thread)mainThread).Interrupt();
                }
            })
            {
                IsBackground = true
            });
        }

        private void StartThreads()
        {
            foreach (var thread in threads)
                thread.Start(Thread.CurrentThread);
        }

        private void ShowProgress()
        {
            var progress = (totalNumberOfChunks - numberOfChunks) * 100 / totalNumberOfChunks;
            Console.Write($"\r{progress}%");
        }
    }
}
