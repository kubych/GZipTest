using GZipTest.CommandDatas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GZipTest.Commands
{
    public class CreateFileCommand : ICommand<CreateFileCommandData>
    {
        private int totalNumberOfBlocks;
        public void Execute(CreateFileCommandData commandData)
        {
            totalNumberOfBlocks = commandData.Size / commandData.BlockSize;
            var data = new byte[commandData.BlockSize];
            var rng = new Random();
            using (var stream = new FileStream(commandData.FileName, FileMode.Create, FileAccess.Write))
            {
                for (var i = 0; i < totalNumberOfBlocks; i++)
                {
                    rng.NextBytes(data);
                    stream.Write(data, 0, data.Length);
                    ShowProgress(i);
                }
            }
        }

        private void ShowProgress(int number)
        {
            var progress = (number + 1) * 100 / totalNumberOfBlocks;
            Console.Write($"\r{progress}%");
        }
    }
}
