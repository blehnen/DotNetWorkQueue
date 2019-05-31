using System.Collections.Generic;
using OpenTracing;
using OpenTracing.Propagation;

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
        public static void Inject(this IMessage message, ITracer tracer, ISpanContext context,
            IStandardHeaders headers)
        {
            var mapping = new DataMappingTextMap();
            tracer.Inject(context, BuiltinFormats.TextMap, mapping);
            message.SetHeader(headers.TraceSpan, mapping);
        }
        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static ISpanContext Extract(this IReceivedMessageInternal message, ITracer tracer, IStandardHeaders headers)
        {
            if (message.Headers.ContainsKey(headers.TraceSpan.Name))
            {
                var mapping =
                   (DataMappingTextMap)message.Headers[headers.TraceSpan.Name];
                return tracer.Extract(BuiltinFormats.TextMap, mapping);
            }
            return null;
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static ISpanContext Extract(this IMessage message, ITracer tracer, IStandardHeaders headers)
        {
            if (message.Headers.ContainsKey(headers.TraceSpan.Name))
            {
                var mapping =
                    (DataMappingTextMap)message.Headers[headers.TraceSpan.Name];
                return tracer.Extract(BuiltinFormats.TextMap, mapping);
            }
            return null;
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="inputHeaders">The input headers.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static ISpanContext Extract(this IDictionary<string, object> inputHeaders, ITracer tracer, IStandardHeaders headers)
        {
            if (inputHeaders.ContainsKey(headers.TraceSpan.Name))
            {
                var mapping =
                    (DataMappingTextMap)inputHeaders[headers.TraceSpan.Name];
                return tracer.Extract(BuiltinFormats.TextMap, mapping);
            }
            return null;
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="inputHeaders">The input headers.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static ISpanContext Extract(this IReadOnlyDictionary<string, object> inputHeaders, ITracer tracer, IStandardHeaders headers)
        {
            if (inputHeaders.ContainsKey(headers.TraceSpan.Name))
            {
                var mapping =
                    (DataMappingTextMap)inputHeaders[headers.TraceSpan.Name];
                return tracer.Extract(BuiltinFormats.TextMap, mapping);
            }
            return null;
        }

        /// <summary>
        /// Extracts the specified tracer.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public static ISpanContext Extract(this IMessageContext context, ITracer tracer, IStandardHeaders headers)
        {
            if (context.Headers.ContainsKey(headers.TraceSpan.Name))
            {
                var mapping =
                    (DataMappingTextMap)context.Headers[headers.TraceSpan.Name];
                return tracer.Extract(BuiltinFormats.TextMap, mapping);
            }
            return null;
        }
        /// <summary>
        /// Adds the common tags.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="data">The data.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public static void AddCommonTags(this ISpan span, IAdditionalMessageData data, IConnectionInformation connectionInformation)
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
        public static void AddMessageIdTag(this ISpan span, IQueueOutputMessage message)
        {
            if(message.SentMessage.MessageId.HasValue)
                span.SetTag("MessageId", message.SentMessage.MessageId.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="message">The message.</param>
        public static void AddMessageIdTag(this ISpan span, IReceivedMessageInternal message)
        {
            if (message.MessageId.HasValue)
                span.SetTag("MessageId", message.MessageId.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="context">The context.</param>
        public static void AddMessageIdTag(this ISpan span, IMessageContext context)
        {
            if (context.MessageId.HasValue)
                span.SetTag("MessageId", context.MessageId.Id.Value.ToString());
        }
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="id">The identifier.</param>
        public static void AddMessageIdTag(this ISpan span, IMessageId id)
        {
            if (id != null && id.HasValue)
                span.SetTag("MessageId", id.Id.Value.ToString());
        }
    }
}
