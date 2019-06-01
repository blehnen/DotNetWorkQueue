using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Message
{
    /// <summary>
    /// Handles receiving a message
    /// </summary>
    internal class ReceiveMessage
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IQueryHandler<ReceiveMessageQuery<SqlConnection, SqlTransaction>, IReceivedMessageInternal> _receiveMessage;
        private readonly IQueryHandler<ReceiveMessageQueryAsync<SqlConnection, SqlTransaction>, Task<IReceivedMessageInternal>> _receiveMessageAsync;
        private readonly ICommandHandler<SetStatusTableStatusCommand> _setStatusCommandHandler;
        private readonly ICancelWork _cancelToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="receiveMessage">The receive message.</param>
        /// <param name="setStatusCommandHandler">The set status command handler.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="receiveMessageAsync">The receive message asynchronous.</param>
        public ReceiveMessage(QueueConsumerConfiguration configuration,
            IQueryHandler<ReceiveMessageQuery<SqlConnection, SqlTransaction>, IReceivedMessageInternal> receiveMessage,
            ICommandHandler<SetStatusTableStatusCommand> setStatusCommandHandler,
            IQueueCancelWork cancelToken, 
            IQueryHandler<ReceiveMessageQueryAsync<SqlConnection, SqlTransaction>, Task<IReceivedMessageInternal>> receiveMessageAsync)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => receiveMessage, receiveMessage);
            Guard.NotNull(() => setStatusCommandHandler, setStatusCommandHandler);
            Guard.NotNull(() => cancelToken, cancelToken);
            Guard.NotNull(() => receiveMessageAsync, receiveMessageAsync);

            _configuration = configuration;
            _receiveMessage = receiveMessage;
            _setStatusCommandHandler = setStatusCommandHandler;
            _cancelToken = cancelToken;
            _receiveMessageAsync = receiveMessageAsync;
        }

        /// <summary>
        /// Returns the next message, if any.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="connectionHolder">The connection.</param>
        /// <param name="noMessageFoundActon">The no message found action.</param>
        /// <returns>
        /// A message if one is found; null otherwise
        /// </returns>
        public IReceivedMessageInternal GetMessage(IMessageContext context, IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand> connectionHolder,
            Action<IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>> noMessageFoundActon)
        {
            //if stopping, exit now
            if (_cancelToken.Tokens.Any(t => t.IsCancellationRequested))
            {
                noMessageFoundActon(connectionHolder);
                return null;
            }

            //ask for the next message
            var receivedTransportMessage =
                _receiveMessage.Handle(new ReceiveMessageQuery<SqlConnection, SqlTransaction>(connectionHolder.Connection,
                    connectionHolder.Transaction,  _configuration.Routes));

            //if no message (null) run the no message action and return
            if (receivedTransportMessage == null)
            {
                noMessageFoundActon(connectionHolder);
                return null;
            }

            //set the message ID on the context for later usage
            context.SetMessageAndHeaders(receivedTransportMessage.MessageId, receivedTransportMessage.Headers);

            //if we are holding open transactions, we need to update the status table in a separate call
            //When not using held transactions, this is part of the de-queue statement and so not needed here

            //TODO - we could consider using a task to update the status table
            //the status table drives nothing internally, however it may drive external processes
            //because of that, we are not returning the message until the status table is updated.
            //we could make this a configurable option in the future?
            if (_configuration.Options().EnableHoldTransactionUntilMessageCommitted &&
                _configuration.Options().EnableStatusTable)
            {
                _setStatusCommandHandler.Handle(
                    new SetStatusTableStatusCommand(
                        (long) receivedTransportMessage.MessageId.Id.Value, QueueStatuses.Processing));
            }
            return receivedTransportMessage;
        }

        /// <summary>
        /// Returns the next message, if any.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="connectionHolder">The connection.</param>
        /// <param name="noMessageFoundActon">The no message found action.</param>
        /// <returns>
        /// A message if one is found; null otherwise
        /// </returns>
        public async Task<IReceivedMessageInternal> GetMessageAsync(IMessageContext context, IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand> connectionHolder,
            Action<IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>> noMessageFoundActon)
        {
            //if stopping, exit now
            if (_cancelToken.Tokens.Any(t => t.IsCancellationRequested))
            {
                noMessageFoundActon(connectionHolder);
                return null;
            }

            //ask for the next message
            var receivedTransportMessage = await 
                _receiveMessageAsync.Handle(new ReceiveMessageQueryAsync<SqlConnection, SqlTransaction>(connectionHolder.Connection,
                    connectionHolder.Transaction, _configuration.Routes)).ConfigureAwait(false);

            //if no message (null) run the no message action and return
            if (receivedTransportMessage == null)
            {
                noMessageFoundActon(connectionHolder);
                return null;
            }

            //set the message ID on the context for later usage
            context.SetMessageAndHeaders(receivedTransportMessage.MessageId, receivedTransportMessage.Headers);

            //if we are holding open transactions, we need to update the status table in a separate call
            //When not using held transactions, this is part of the de-queue statement and so not needed here

            //TODO - we could consider using a task to update the status table
            //the status table drives nothing internally, however it may drive external processes
            //because of that, we are not returning the message until the status table is updated.
            //we could make this a configurable option in the future?
            if (_configuration.Options().EnableHoldTransactionUntilMessageCommitted &&
                _configuration.Options().EnableStatusTable)
            {
                _setStatusCommandHandler.Handle(
                    new SetStatusTableStatusCommand(
                        (long)receivedTransportMessage.MessageId.Id.Value, QueueStatuses.Processing));
            }
            return receivedTransportMessage;
        }
    }
}
