// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
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
