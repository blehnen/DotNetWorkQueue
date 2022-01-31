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
        public ConsoleExecuteActions Action { get; }
        public dynamic Target { get; }
    }
}
