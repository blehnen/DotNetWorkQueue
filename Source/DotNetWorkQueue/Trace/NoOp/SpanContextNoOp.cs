using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing;

namespace DotNetWorkQueue.Trace.NoOp
{
    internal sealed class SpanContextNoOp: ISpanContext
    {
        public SpanContextNoOp()
        {
            TraceId = string.Empty;
            SpanId = string.Empty;
        }
        public IEnumerable<KeyValuePair<string, string>> GetBaggageItems()
        {
           return new List<KeyValuePair<string, string>>();
        }

        public string TraceId { get; }
        public string SpanId { get; }
    }
}
