// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using OpenTracing;
using OpenTracing.Tag;

namespace DotNetWorkQueue.Trace.NoOp
{
    internal sealed class SpanBuilderNoOp: ISpanBuilder
    {
        public ISpanBuilder AsChildOf(ISpanContext parent)
        {
            return this;
        }

        public ISpanBuilder AsChildOf(ISpan parent)
        {
            return this;
        }

        public ISpanBuilder AddReference(string referenceType, ISpanContext referencedContext)
        {
            return this;
        }

        public ISpanBuilder IgnoreActiveSpan()
        {
            return this;
        }

        public ISpanBuilder WithTag(string key, string value)
        {
            return this;
        }

        public ISpanBuilder WithTag(string key, bool value)
        {
            return this;
        }

        public ISpanBuilder WithTag(string key, int value)
        {
            return this;
        }

        public ISpanBuilder WithTag(string key, double value)
        {
            return this;
        }

        public ISpanBuilder WithTag(BooleanTag tag, bool value)
        {
            return this;
        }

        public ISpanBuilder WithTag(IntOrStringTag tag, string value)
        {
            return this;
        }

        public ISpanBuilder WithTag(IntTag tag, int value)
        {
            return this;
        }

        public ISpanBuilder WithTag(StringTag tag, string value)
        {
            return this;
        }

        public ISpanBuilder WithStartTimestamp(DateTimeOffset timestamp)
        {
            return this;
        }

        public IScope StartActive()
        {
            return new ScopeNoOp();
        }

        public IScope StartActive(bool finishSpanOnDispose)
        {
            return new ScopeNoOp();
        }

        public ISpan Start()
        {
            return new SpanNoOp();
        }
    }
}
