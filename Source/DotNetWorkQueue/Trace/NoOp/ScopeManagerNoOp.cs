using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing;

namespace DotNetWorkQueue.Trace.NoOp
{
    internal sealed class ScopeManagerNoOp : IScopeManager
    {
        public ScopeManagerNoOp()
        {
            Active = new ScopeNoOp();
        }

        public IScope Activate(ISpan span, bool finishSpanOnDispose)
        {
            return new ScopeNoOp();
        }

        public IScope Active { get; }
    }
}
