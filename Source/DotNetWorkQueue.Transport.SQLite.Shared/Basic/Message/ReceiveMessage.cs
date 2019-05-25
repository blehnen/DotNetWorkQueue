using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.Message
{
    /// <summary>
    /// Handles receiving a message
    /// </summary>
    internal class ReceiveMessage
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IQueryHandler<ReceiveMessageQuery<IDbConnection, IDbTransaction>, IReceivedMessageInternal> _receiveMessage;
        private readonly IQueryHandler<ReceiveMessageQueryAsync<IDbConnection, IDbTransaction>, Task<IReceivedMessageInternal>> _receiveMessageAsync;
        private readonly ICancelWork _cancelToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="receiveMessage">The receive message.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="receiveMessageAsync">The receive message asynchronous.</param>
        public ReceiveMessage(QueueConsumerConfiguration configuration,
            IQueryHandler<ReceiveMessageQuery<IDbConnection, IDbTransaction>, IReceivedMessageInternal> receiveMessage,
            IQueueCancelWork cancelToken, 
            IQueryHandler<ReceiveMessageQueryAsync<IDbConnection, IDbTransaction>, Task<IReceivedMessageInternal>> receiveMessageAsync)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => receiveMessage, receiveMessage);
            Guard.NotNull(() => cancelToken, cancelToken);
            Guard.NotNull(() => receiveMessageAsync, receiveMessageAsync);

            _configuration = configuration;
            _receiveMessage = receiveMessage;
            _cancelToken = cancelToken;
            _receiveMessageAsync = receiveMessageAsync;
        }

        /// <summary>
        /// Returns the next message, if any.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A message if one is found; null otherwise
        /// </returns>
        public IReceivedMessageInternal GetMessage(IMessageContext context)
        {
            //if stopping, exit now
            if (_cancelToken.Tokens.Any(t => t.IsCancellationRequested))
            {
                return null;
            }

            //check for a specific MessageID to pull
            IMessageId messageId = null;
            var rpc = context.Get(_configuration.HeaderNames.StandardHeaders.RpcContext);
            if (rpc?.MessageId != null && rpc.MessageId.HasValue)
            {
                messageId = rpc.MessageId;
            }

            //ask for the next message, or a specific message if we have a messageID
            var receivedTransportMessage =
                _receiveMessage.Handle(new ReceiveMessageQuery<IDbConnection, IDbTransaction>(null, null, messageId, _configuration.Routes));

            //if no message (null) run the no message action and return
            if (receivedTransportMessage == null)
            {
                return null;
            }

            //set the message ID on the context for later usage
            context.SetMessageAndHeaders(receivedTransportMessage.MessageId, receivedTransportMessage.Headers);

            return receivedTransportMessage;
        }

        /// <summary>
        /// Returns the next message, if any.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A message if one is found; null otherwise
        /// </returns>
        public async Task<IReceivedMessageInternal> GetMessageAsync(IMessageContext context)
        {
            //if stopping, exit now
            if (_cancelToken.Tokens.Any(t => t.IsCancellationRequested))
            {
                return null;
            }

            //check for a specific MessageID to pull
            IMessageId messageId = null;
            var rpc = context.Get(_configuration.HeaderNames.StandardHeaders.RpcContext);
            if (rpc?.MessageId != null && rpc.MessageId.HasValue)
            {
                messageId = rpc.MessageId;
            }

            //ask for the next message, or a specific message if we have a messageID
            var receivedTransportMessage = await 
                _receiveMessageAsync.Handle(new ReceiveMessageQueryAsync<IDbConnection, IDbTransaction>(null, null, messageId, _configuration.Routes)).ConfigureAwait(false);

            //if no message (null) run the no message action and return
            if (receivedTransportMessage == null)
            {
                return null;
            }

            //set the message ID on the context for later usage
            context.SetMessageAndHeaders(receivedTransportMessage.MessageId, receivedTransportMessage.Headers);

            return receivedTransportMessage;
        }
    }
}
