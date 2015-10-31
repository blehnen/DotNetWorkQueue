// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
// ---------------------------------------------------------------------

using System.Collections.Generic;
using System.Reflection;
namespace ConsoleShared
{
    public class ConsoleExecutingMethod
    {
        public ConsoleExecutingMethod(string nameSpace, Assembly targetAssembly, IEnumerable<ParameterInfo> parameters, bool async)
        {
            TargetAssembly = targetAssembly;
            Parameters = parameters;
            Namespace = nameSpace;
            Async = async;
        }
        public Assembly TargetAssembly { get; private set; }
        public IEnumerable<ParameterInfo> Parameters { get; private set; }
        public string Namespace { get; private set; }
        public bool Async { get; private set; }
    }
}
