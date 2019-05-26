using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing;

namespace DotNetWorkQueue.Trace.NoOp
{
    internal sealed class ScopeNoOp: IScope
    {
        public ScopeNoOp()
        {
            Span = new SpanNoOp();
        }
        public void Dispose()
        {
            
        }

        public ISpan Span { get; }
    }
}
