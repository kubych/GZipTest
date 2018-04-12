using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZipTest.CommandDatas
{
    public class CreateFileCommandData
    {
        public string FileName { get; set; }
        public int BlockSize { get; set; }
        public int Size { get; set; }
    }
}
