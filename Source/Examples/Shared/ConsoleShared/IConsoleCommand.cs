// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
// ---------------------------------------------------------------------

namespace ConsoleShared
{
    public interface IConsoleCommand
    {
        ConsoleExecuteResult Info { get; }
        ConsoleExecuteResult Help();
        ConsoleExecuteResult Example(string command);
    }
}
