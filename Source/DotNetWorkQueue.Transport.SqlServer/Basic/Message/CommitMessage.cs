using System;
using System.Data.SqlClient;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Message
{
    /// <summary>
    /// Commits a processed message
    /// </summary>
    internal class CommitMessage
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICommandHandler<DeleteStatusTableStatusCommand> _deleteStatusCommandHandler;
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, long> _deleteMessageCommand;
        private readonly ICommandHandlerWithOutput<DeleteTransactionalMessageCommand, long> _deleteTransactionalMessageCommand;
        private readonly IConnectionHeader<SqlConnection, SqlTransaction, SqlCommand> _headers;
        private readonly ILog _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="deleteStatusCommandHandler">The delete status command handler.</param>
        /// <param name="deleteMessageCommand">The delete message command.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="deleteTransactionalMessageCommand">The delete transactional message command.</param>
        /// <param name="log">The log.</param>
        public CommitMessage(QueueConsumerConfiguration configuration, 
            ICommandHandler<DeleteStatusTableStatusCommand> deleteStatusCommandHandler,
            ICommandHandlerWithOutput<DeleteMessageCommand, long> deleteMessageCommand,
            IConnectionHeader<SqlConnection, SqlTransaction, SqlCommand> headers, 
            ICommandHandlerWithOutput<DeleteTransactionalMessageCommand, long> deleteTransactionalMessageCommand,
            ILogFactory log)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => deleteStatusCommandHandler, deleteStatusCommandHandler);
            Guard.NotNull(() => deleteMessageCommand, deleteMessageCommand);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => deleteTransactionalMessageCommand, deleteTransactionalMessageCommand);
            Guard.NotNull(() => log, log);

            _configuration = configuration;
            _deleteStatusCommandHandler = deleteStatusCommandHandler;
            _deleteMessageCommand = deleteMessageCommand;
            _headers = headers;
            _deleteTransactionalMessageCommand = deleteTransactionalMessageCommand;
            _log = log.Create();
        }
        /// <summary>
        /// Commits the message, via the held transaction
        /// </summary>
        /// <param name="context">The context.</param>
        public void CommitForTransaction(IMessageContext context)
        {
            var connection = context.Get(_headers.Connection);

            //if transaction held
            if (connection.Connection == null || connection.Transaction == null) return;

            //delete the message, and then commit the transaction
            _deleteTransactionalMessageCommand.Handle(new DeleteTransactionalMessageCommand((long)context.MessageId.Id.Value, context));

            try
            {
                connection.Transaction.Commit();
            }
            catch (Exception e)
            {
                _log.ErrorException("Failed to commit a transaction; this might be due to a DB timeout", e);

                //don't attempt to use the transaction again at this point.
                connection.Transaction = null;

                throw;
            }

            //ensure that transaction won't be used anymore
            connection.Transaction.Dispose();
            connection.Transaction = null;

            if (_configuration.Options().EnableStatusTable)
            {
                _deleteStatusCommandHandler.Handle(new DeleteStatusTableStatusCommand((long)context.MessageId.Id.Value));
            }
        }
        /// <summary>
        /// Commits the processed message, by deleting the message
        /// </summary>
        /// <param name="context">The context.</param>
        public void Commit(IMessageContext context)
        {
            if (context.MessageId != null && context.MessageId.HasValue)
            {
                _deleteMessageCommand.Handle(new DeleteMessageCommand((long)context.MessageId.Id.Value));
            }
        }
    }
}
