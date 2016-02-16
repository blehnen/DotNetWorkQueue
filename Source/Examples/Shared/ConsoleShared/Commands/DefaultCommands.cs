// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
// ---------------------------------------------------------------------

//All rights reserved.

//MIT License

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
// ---------------------------------------------------------------------

using System.Text;

namespace ConsoleShared.Commands
{
    public class DefaultCommands : IConsoleCommand
    {
        public ConsoleExecuteResult Quit()
        {
            return new ConsoleExecuteResult(string.Empty, new ConsoleExecuteAction(ConsoleExecuteActions.Exit, null));
        }
        public ConsoleExecuteResult Exit()
        {
            return new ConsoleExecuteResult(string.Empty, new ConsoleExecuteAction(ConsoleExecuteActions.Exit, null));
        }

        public ConsoleExecuteResult Info => new ConsoleExecuteResult(ConsoleFormatting.FixedLength("DefaultCommands", "Default commands such as quit/help/macro"));

        public ConsoleExecuteResult Help()
        {
            var help = new StringBuilder();
            help.AppendLine("Default commands are");
            help.AppendLine("");
            help.AppendLine(ConsoleFormatting.FixedLength("Help", "Displays command list"));
            help.AppendLine(ConsoleFormatting.FixedLength("Example name", "Displays example syntax for a command"));
            help.AppendLine(ConsoleFormatting.FixedLength("StartMacro", "Starts capturing a new macro"));
            help.AppendLine(ConsoleFormatting.FixedLength("CancelMacro", "Cancels capture of a macro started with 'StartMacro'"));
            help.AppendLine(ConsoleFormatting.FixedLength("SaveMacro Name", "Saves a macro started with 'StartMacro' to the indicated file"));
            help.AppendLine(ConsoleFormatting.FixedLength("RunMacro Name", "Runs the macro"));
            help.AppendLine(ConsoleFormatting.FixedLength("Quit", "Exits the application"));
            help.AppendLine("");
            help.AppendLine("To get help for other name spaces, use <namespace>.Help");
            return new ConsoleExecuteResult(help.ToString());
        }

        public virtual ConsoleExecuteResult Example(string command)
        {
            switch (command)
            {
                case "Help":
                    return new ConsoleExecuteResult("Help");
                case "Example":
                    return new ConsoleExecuteResult("Example commandname");
                case "StartMacro":
                    return new ConsoleExecuteResult("StartMacro");
                case "SaveMacro":
                    return new ConsoleExecuteResult("SaveMacro testmacro");
                case "RunMacro":
                    return new ConsoleExecuteResult("RunMacro testmacro");
                case "CancelMacro":
                    return new ConsoleExecuteResult("CancelMacro");
                case "Quit":
                    return new ConsoleExecuteResult("Quit");
            }
            return new ConsoleExecuteResult("Command not found");
        }

        public ConsoleExecuteResult StartMacro()
        {
            return new ConsoleExecuteResult("Starting capture", new ConsoleExecuteAction(ConsoleExecuteActions.StartMacro, null));
        }

        public ConsoleExecuteResult CancelMacro()
        {
            return new ConsoleExecuteResult("canceling capture", new ConsoleExecuteAction(ConsoleExecuteActions.CancelMacro, null));
        }

        public ConsoleExecuteResult SaveMacro(string name)
        {
            return new ConsoleExecuteResult($"Saving macro {name}", new ConsoleExecuteAction(ConsoleExecuteActions.SaveMacro, name));
        }

        public ConsoleExecuteResult RunMacro(string name)
        {
            return new ConsoleExecuteResult($"Running macro {name}", new ConsoleExecuteAction(ConsoleExecuteActions.RunMacro, name));
        }
    }
}
