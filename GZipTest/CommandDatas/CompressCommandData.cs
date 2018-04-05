using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZipTest.CommandDatas
{
    public class CompressCommandData : ExceptionCommandData
    {
        public string InputFileName { get; set; }
        public string OutputFileName { get; set; }
    }
}
