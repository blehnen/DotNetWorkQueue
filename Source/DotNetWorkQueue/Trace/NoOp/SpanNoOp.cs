using System;
using System.Collections.Generic;
using OpenTracing;
using OpenTracing.Tag;

namespace DotNetWorkQueue.Trace.NoOp
{
    internal sealed class SpanNoOp: ISpan
    {
        public SpanNoOp()
        {
            Context = new SpanContextNoOp();
        }
        public ISpan SetTag(string key, string value)
        {
            return this;
        }

        public ISpan SetTag(string key, bool value)
        {
            return this;
        }

        public ISpan SetTag(string key, int value)
        {
            return this;
        }

        public ISpan SetTag(string key, double value)
        {
            return this;
        }

        public ISpan SetTag(BooleanTag tag, bool value)
        {
            return this;
        }

        public ISpan SetTag(IntOrStringTag tag, string value)
        {
            return this;
        }

        public ISpan SetTag(IntTag tag, int value)
        {
            return this;
        }

        public ISpan SetTag(StringTag tag, string value)
        {
            return this;
        }

        public ISpan Log(IEnumerable<KeyValuePair<string, object>> fields)
        {
            return this;
        }

        public ISpan Log(DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>> fields)
        {
            return this;
        }

        public ISpan Log(string @event)
        {
            return this;
        }

        public ISpan Log(DateTimeOffset timestamp, string @event)
        {
            return this;
        }

        public ISpan SetBaggageItem(string key, string value)
        {
            return this;
        }

        public string GetBaggageItem(string key)
        {
            return string.Empty;
        }

        public ISpan SetOperationName(string operationName)
        {
            return this;
        }

        public void Finish()
        {
            
        }

        public void Finish(DateTimeOffset finishTimestamp)
        {
            
        }

        public ISpanContext Context { get; }
    }
}
