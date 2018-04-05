using GZipTest.CommandDatas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest.Commands
{
    public class CatchExceptionCommandDecorator<T> : ICommand<T>
    {
        private const string GenericErrorMessage = "Cannot complete the operation.";

        private readonly ICommand<T> command;

        public CatchExceptionCommandDecorator(ICommand<T> command)
        {
            this.command = command;
        }

        public void Execute(T commandData)
        {
            try
            {
                command.Execute(commandData);
            }
            catch (ThreadInterruptedException e)
            {
                if (commandData is ExceptionCommandData)
                {
                    var exception = commandData as ExceptionCommandData;
                    Output(exception.Error, exception.Exception);
                }
                else
                {
                    Output(GenericErrorMessage, e);
                }
            }
            catch (Exception e)
            {
                Output(GenericErrorMessage, e);
            }
        }

        private void Output(string message, Exception e)
        {
            Console.WriteLine($"\r[Error] {message}");
            Console.WriteLine($"Exception: '{e.Message}'");
            Console.WriteLine($"StackTrace: '{e.StackTrace}");
        }
    }
}
