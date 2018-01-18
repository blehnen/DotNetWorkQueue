using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class SendMessageCommandBatchHandler :
        ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>
    {
        private readonly ICompositeSerialization _serializer;
        private readonly EnqueueBatchLua _enqueue;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly IGetMessageIdFactory _messageIdFactory;
        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly RedisHeaders _redisHeaders;
        private readonly ISendBatchSize _sendBatchSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandBatchHandler" /> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="enqueue">The enqueue.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="messageIdFactory">The message identifier factory.</param>
        /// <param name="sentMessageFactory">The sent message factory.</param>
        /// <param name="redisHeaders">The redis headers.</param>
        /// <param name="sendBatchSize">Size of the send batch.</param>
        public SendMessageCommandBatchHandler(
            ICompositeSerialization serializer,
            EnqueueBatchLua enqueue,
            IUnixTimeFactory unixTimeFactory,
            IGetMessageIdFactory messageIdFactory,
            ISentMessageFactory sentMessageFactory,
            RedisHeaders redisHeaders,
            ISendBatchSize sendBatchSize)
        {
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => enqueue, enqueue);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => messageIdFactory, messageIdFactory);
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);
            Guard.NotNull(() => redisHeaders, redisHeaders);
            Guard.NotNull(() => sendBatchSize, sendBatchSize);

            _serializer = serializer;
            _enqueue = enqueue;
            _messageIdFactory = messageIdFactory;
            _sentMessageFactory = sentMessageFactory;
            _redisHeaders = redisHeaders;
            _sendBatchSize = sendBatchSize;
            _unixTimeFactory = unixTimeFactory;
        }

        /// <inheritdoc />
        public QueueOutputMessages Handle(SendMessageCommandBatch commandSend)
        {
            var rc = new ConcurrentBag<IQueueOutputMessage>();
            var splitList = commandSend.Messages.Partition(_sendBatchSize.BatchSize(commandSend.Messages.Count))
                .Select(x => x.ToList())
                .ToList();
            Parallel.ForEach(splitList, m =>
            {
                var meta =
                    _serializer.InternalSerializer.ConvertToBytes(
                        new RedisMetaData(_unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds()));
                var sentMessages = SendMessages(m, meta);
                foreach (var s in sentMessages)
                {
                    rc.Add(s);
                }
            });
            return new QueueOutputMessages(rc.ToList());
        }

        /// <summary>
        /// Sends the messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="meta">The meta.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException"></exception>
        private IEnumerable<QueueOutputMessage> SendMessages(
            IReadOnlyCollection<QueueMessage<IMessage, IAdditionalMessageData>> messages, byte[] meta)
        {
            var messagesToSend = BatchMessageShared.CreateMessagesToSend(_redisHeaders, messages, meta,
                _unixTimeFactory, _messageIdFactory, _serializer);
            try
            {
                var result = _enqueue.Execute(messagesToSend);
                if (result != null)
                {
                    return BatchMessageShared.ProcessSentMessages(result, messages.Count, _sentMessageFactory);
                }
                throw new DotNetWorkQueueException("Returned no data when enqueue messages");
            }
            catch (Exception error)
            {
                var output = new List<QueueOutputMessage>(messages.Count);
                output.AddRange(
                    messages.Select(
                        message =>
                            new QueueOutputMessage(_sentMessageFactory.Create(null, message.MessageData.CorrelationId),
                                error)));
                return output;
            }
        }
    }
}
