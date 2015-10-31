// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
// ---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
namespace ConsoleShared
{
    public class CommandList: Dictionary<ConsoleExecutingAssembly, Dictionary<string,
                    ConsoleExecutingMethod>>, IDisposable
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            foreach (var instance in Keys.Where(instance => instance.Instance is IDisposable))
            {
                ((IDisposable) instance.Instance).Dispose();
            }
        }
    }
}
