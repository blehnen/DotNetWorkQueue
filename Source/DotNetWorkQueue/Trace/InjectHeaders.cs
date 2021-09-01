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
using System.Diagnostics;
using System.Linq;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

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
        public static void Inject(this IMessage message, ActivitySource tracer, ActivityContext context,
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
        public static ActivityContext Extract(this IReceivedMessageInternal message, ActivitySource tracer,
            IStandardHeaders headers)
        {
            TextMapPropagator propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, message.Headers, ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            return parentContext.ActivityContext;
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static ActivityContext Extract(this IMessage message, ActivitySource tracer, IStandardHeaders headers)
        {
            TextMapPropagator propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, new ReadOnlyDictionary<string, object>(message.Headers), ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            return parentContext.ActivityContext;
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="inputHeaders">The input headers.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static ActivityContext Extract(this IDictionary<string, object> inputHeaders, ActivitySource tracer, IStandardHeaders headers)
        {
            TextMapPropagator propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, new ReadOnlyDictionary<string, object>(inputHeaders), ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            return parentContext.ActivityContext;
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="inputHeaders">The input headers.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static ActivityContext Extract(this IReadOnlyDictionary<string, object> inputHeaders, ActivitySource tracer, IStandardHeaders headers)
        {
            TextMapPropagator propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, inputHeaders, ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            return parentContext.ActivityContext;
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static ActivityContext Extract(this IMessageContext context, ActivitySource tracer, IStandardHeaders headers)
        {
            TextMapPropagator propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, context.Headers, ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            return parentContext.ActivityContext;
        }
        /// <summary>
        /// Adds the common tags.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="data">The data.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public static void AddCommonTags(this Activity span, IAdditionalMessageData data, IConnectionInformation connectionInformation)
        {
            span.SetTag("Server", connectionInformation.Server);
            span.SetTag("Queue", connectionInformation.QueueName);
            span.SetTag("CorrelationId", data.CorrelationId.ToString());
            if(!string.IsNullOrEmpty(data.Route))
                span.SetTag("Route", data.Route);

            foreach(var userTag in data.TraceTags)
            {
                span.SetTag(userTag.Key, userTag.Value);
            }
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="message">The message.</param>
        public static void AddMessageIdTag(this Activity span, IQueueOutputMessage message)
        {
            if(message.SentMessage.MessageId.HasValue)
                span.SetTag("MessageId", message.SentMessage.MessageId.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="message">The message.</param>
        public static void AddMessageIdTag(this Activity span, IReceivedMessageInternal message)
        {
            if (message.MessageId.HasValue)
                span.SetTag("MessageId", message.MessageId.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="context">The context.</param>
        public static void AddMessageIdTag(this Activity span, IMessageContext context)
        {
            if (context.MessageId.HasValue)
                span.SetTag("MessageId", context.MessageId.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="id">The identifier.</param>
        public static void AddMessageIdTag(this Activity span, IMessageId id)
        {
            if (id != null && id.HasValue)
                span.SetTag("MessageId", id.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="id">The identifier.</param>
        public static void AddMessageIdTag(this Activity span, string id)
        {
            if (!string.IsNullOrEmpty(id))
                span.SetTag("MessageId", id);
        }
    }
}
