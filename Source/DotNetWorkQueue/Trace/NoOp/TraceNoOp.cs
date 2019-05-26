using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing;
using OpenTracing.Propagation;

namespace DotNetWorkQueue.Trace.NoOp
{
    internal sealed class TraceNoOp: ITracer
    {
        public TraceNoOp()
        {
            ScopeManager = new ScopeManagerNoOp();
            ActiveSpan = new SpanNoOp();
        }
        public ISpanBuilder BuildSpan(string operationName)
        {
            return new SpanBuilderNoOp();
        }

        public void Inject<TCarrier>(ISpanContext spanContext, IFormat<TCarrier> format, TCarrier carrier)
        {
           
        }

        public ISpanContext Extract<TCarrier>(IFormat<TCarrier> format, TCarrier carrier)
        {
           return new SpanContextNoOp();
        }

        public IScopeManager ScopeManager { get; }
        public ISpan ActiveSpan { get; }
    }
}
