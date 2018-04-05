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
    public class CompressCommand : ICommand<CompressCommandData>
    {
        private readonly int chunkSize;
        private readonly int numberOfThreads;

        private ThreadSafeQueue<Chunk> chunksQueue;
        private ThreadSafeQueue<CompressedChunk> compressedChunksQueue;

        private int numberOfChunks;
        private int totalNumberOfChunks;
        private EventWaitHandle waitHandler;

        public CompressCommand()
        {
            numberOfThreads = Environment.ProcessorCount;
            if (!int.TryParse(ConfigurationManager.AppSettings["chunkSize"], out chunkSize))
                chunkSize = 1024 * 64;

            chunksQueue = new ThreadSafeQueue<Chunk>(this.numberOfThreads);
            compressedChunksQueue = new ThreadSafeQueue<CompressedChunk>(this.numberOfThreads);
        }

        public void Execute(CompressCommandData commandData)
        {
            var fileInfo = new FileInfo(commandData.InputFileName);
            numberOfChunks = (int)Math.Ceiling((decimal)fileInfo.Length / chunkSize);
            totalNumberOfChunks = numberOfChunks;
            waitHandler = new ManualResetEvent(false);

            for (var i = 0; i < numberOfThreads; i++)
            {
                var thread = new Thread((mainThread) =>
                {
                    try
                    {
                        while (numberOfChunks > 0)
                        {
                            var chunk = chunksQueue.Dequeue();
                            var compressed = chunk.Compress();
                            compressedChunksQueue.Enqueue(compressed);
                        }
                    }
                    catch (Exception e)
                    {
                        commandData.Error = "Error during compressing a chunk of data in thread";
                        commandData.Exception = e;
                        ((Thread)mainThread).Interrupt();
                    }
                })
                {
                    IsBackground = true
                };
                thread.Start(Thread.CurrentThread);
            }

            var writerThread = new Thread((mainThread) =>
            {
                try
                {
                    using (var writer = new FileStream(commandData.OutputFileName, FileMode.Create, FileAccess.Write))
                    {
                        writer.Write(BitConverter.GetBytes(numberOfChunks), 0, sizeof(int));
                        while (numberOfChunks > 0)
                        {
                            var compressed = compressedChunksQueue.Dequeue();

                            writer.Write(BitConverter.GetBytes(compressed.Number), 0, sizeof(int));
                            writer.Write(BitConverter.GetBytes(compressed.InitialSize), 0, sizeof(int));
                            writer.Write(BitConverter.GetBytes(compressed.Size), 0, sizeof(int));
                            writer.Write(compressed.Data, 0, compressed.Size);

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
            };
            writerThread.Start(Thread.CurrentThread);

            using (var fileStream = new FileStream(commandData.InputFileName, FileMode.Open, FileAccess.Read))
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

        private void ShowProgress()
        {
            var progress = (totalNumberOfChunks - numberOfChunks) * 100 / totalNumberOfChunks;
            Console.Write($"\r{progress}%");
        }
    }
}
