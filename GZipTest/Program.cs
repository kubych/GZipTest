using GZipTest.CommandDatas;
using GZipTest.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Input agruments needs to be specified. Format: 'GZipTest.exe compress/decompress input output' ");
                return;
            }

            switch (args[0])
            {
                case "compress":
                    var compress = new CatchExceptionCommandDecorator<CompressCommandData>(new CompressCommand());
                    //var compress = new CatchExceptionCommandDecorator<CompressInSingleThreadCommandData>(new CompressInSingleThreadCommand());
                    compress.Execute(new CompressCommandData { InputFileName = args[1], OutputFileName = args[2] });
                    break;

                case "decompress":
                    var decompress = new CatchExceptionCommandDecorator<DecompressCommandData>(new DecompressCommand());
                    decompress.Execute(new DecompressCommandData { InputFileName = args[1], OutputFileName = args[2] });
                    break;

                case "create":
                    var create = new CatchExceptionCommandDecorator<CreateFileCommandData>(new CreateFileCommand());
                    Console.WriteLine("Creating file...");
                    create.Execute(new CreateFileCommandData { FileName = args[1], BlockSize = 1024, Size = 1024 * 1024 * 1024 });
                    break;

                default:
                    Console.WriteLine($"First agrument should be either 'compress' or 'decompress'");
                    return;
            }
        }
    }
}
