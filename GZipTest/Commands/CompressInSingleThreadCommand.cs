using GZipTest.CommandDatas;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace GZipTest.Commands
{
    public class CompressInSingleThreadCommand : ICommand<CompressInSingleThreadCommandData>
    {
        public void Execute(CompressInSingleThreadCommandData commandData)
        {
            using (var fsIn = File.OpenRead(commandData.InputFileName))
            {
                using (var zip = new GZipStream(File.Create(commandData.OutputFileName), CompressionMode.Compress))
                {
                    var buffer = new byte[1024 * 1024 * 16];
                    int bytesRead;

                    while ((bytesRead = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        zip.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }
    }
}
