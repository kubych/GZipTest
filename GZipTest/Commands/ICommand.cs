using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZipTest.Commands
{
    public interface ICommand<T>
    {
        void Execute(T commandData);
    }
}
