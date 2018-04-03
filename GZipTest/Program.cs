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

            var gzip = new GZip();
            switch (args[0])
            {
                case "compress":
                    Console.WriteLine("Comressing...");
                    gzip.Compress(args[1], args[2]);
                    break;

                case "decompress":
                    Console.WriteLine("Decompressing...");
                    gzip.Decompress(args[1], args[2]);
                    break;

                default:
                    Console.WriteLine($"First agrument should be either 'compress' or 'decompress'");
                    return;
            }
        }
    }
}
