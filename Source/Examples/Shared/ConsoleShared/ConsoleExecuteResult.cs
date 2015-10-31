// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
// ---------------------------------------------------------------------

namespace ConsoleShared
{
    public class ConsoleExecuteResult
    {
        public ConsoleExecuteResult(string message, ConsoleExecuteAction action = null)
        {
            Message = message;
            Action = action;
        }
        public string Message { get; }
        public ConsoleExecuteAction Action { get; }
        public override string ToString()
        {
            return Message;
        }
    }

    public enum ConsoleExecuteActions
    {
        None = 0,
        Exit = 1,
        StartProcess = 2,
        StatusUri = 3,
        StartMacro = 4,
        SaveMacro = 5,
        RunMacro = 6,
        RunCommand = 7,
        CancelMacro = 8
    }

    public class ConsoleExecuteAction
    {
        public ConsoleExecuteAction(ConsoleExecuteActions action, dynamic target)
        {
            Action = action;
            Target = target;
        }
        public ConsoleExecuteActions Action { get; private set; }
        public dynamic Target { get; private set; }
    }
}
