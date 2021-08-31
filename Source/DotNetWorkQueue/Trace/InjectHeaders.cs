// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace DotNetWorkQueue.Trace
{
    /// <summary>
    /// Extensions for setting trace properties
    /// </summary>
    public static class TraceExtensions
    {
        /// <summary>
        /// Injects the specified tracer.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="context">The context.</param>
        /// <param name="headers">The headers.</param>
        public static void Inject(this IMessage message, Tracer tracer, SpanContext context,
            IStandardHeaders headers)
        {
            var mapping = Propagators.DefaultTextMapPropagator;;
            mapping.Inject(new PropagationContext(context, Baggage.Current), message.Headers, InjectTraceContextIntoBasicProperties);

            //tracer.Inject(context, BuiltinFormats.TextMap, mapping);
            //message.SetHeader(headers.TraceSpan, mapping);
        }

        private static void InjectTraceContextIntoBasicProperties(IDictionary<string, object> props, string key, string value)
        {
            props[key] = value;
        }

        private static IEnumerable<string> ExtractTraceContextFromBasicProperties(
            IReadOnlyDictionary<string, object> props, string key)
        {
            if (props.TryGetValue(key, out var value))
            {
                var bytes = value as string;
                return new[] {bytes};
            }
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static SpanContext Extract(this IReceivedMessageInternal message, Tracer tracer,
            IStandardHeaders headers)
        {
            TextMapPropagator propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, message.Headers, ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            return new SpanContext(parentContext.ActivityContext);
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static SpanContext Extract(this IMessage message, Tracer tracer, IStandardHeaders headers)
        {
            TextMapPropagator propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, new ReadOnlyDictionary<string, object>(message.Headers), ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            return new SpanContext(parentContext.ActivityContext);
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="inputHeaders">The input headers.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static SpanContext Extract(this IDictionary<string, object> inputHeaders, Tracer tracer, IStandardHeaders headers)
        {
            TextMapPropagator propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, new ReadOnlyDictionary<string, object>(inputHeaders), ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            return new SpanContext(parentContext.ActivityContext);
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="inputHeaders">The input headers.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static SpanContext Extract(this IReadOnlyDictionary<string, object> inputHeaders, Tracer tracer, IStandardHeaders headers)
        {
            TextMapPropagator propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, inputHeaders, ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            return new SpanContext(parentContext.ActivityContext);
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static SpanContext Extract(this IMessageContext context, Tracer tracer, IStandardHeaders headers)
        {
            TextMapPropagator propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, context.Headers, ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            return new SpanContext(parentContext.ActivityContext);
        }
        /// <summary>
        /// Adds the common tags.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="data">The data.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public static void AddCommonTags(this TelemetrySpan span, IAdditionalMessageData data, IConnectionInformation connectionInformation)
        {
            span.SetAttribute("Server", connectionInformation.Server);
            span.SetAttribute("Queue", connectionInformation.QueueName);
            span.SetAttribute("CorrelationId", data.CorrelationId.ToString());
            if(!string.IsNullOrEmpty(data.Route))
                span.SetAttribute("Route", data.Route);

            foreach(var userTag in data.TraceTags)
            {
                span.SetAttribute(userTag.Key, userTag.Value);
            }
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="message">The message.</param>
        public static void AddMessageIdTag(this TelemetrySpan span, IQueueOutputMessage message)
        {
            if(message.SentMessage.MessageId.HasValue)
                span.SetAttribute("MessageId", message.SentMessage.MessageId.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="message">The message.</param>
        public static void AddMessageIdTag(this TelemetrySpan span, IReceivedMessageInternal message)
        {
            if (message.MessageId.HasValue)
                span.SetAttribute("MessageId", message.MessageId.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="context">The context.</param>
        public static void AddMessageIdTag(this TelemetrySpan span, IMessageContext context)
        {
            if (context.MessageId.HasValue)
                span.SetAttribute("MessageId", context.MessageId.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="id">The identifier.</param>
        public static void AddMessageIdTag(this TelemetrySpan span, IMessageId id)
        {
            if (id != null && id.HasValue)
                span.SetAttribute("MessageId", id.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="id">The identifier.</param>
        public static void AddMessageIdTag(this TelemetrySpan span, string id)
        {
            if (!string.IsNullOrEmpty(id))
                span.SetAttribute("MessageId", id);
        }
    }
}
