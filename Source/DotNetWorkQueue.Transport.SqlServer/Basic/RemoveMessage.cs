// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System.Data.SqlClient;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Basic.Message;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Removes a message from storage
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IRemoveMessage" />
    public class RemoveMessage: IRemoveMessage
    {
        private readonly bool _useTransactions;
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
        public RemoveMessage(QueueConsumerConfiguration configuration,
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

            _useTransactions = configuration.Options().EnableHoldTransactionUntilMessageCommitted;
            _configuration = configuration;
            _deleteStatusCommandHandler = deleteStatusCommandHandler;
            _deleteMessageCommand = deleteMessageCommand;
            _headers = headers;
            _deleteTransactionalMessageCommand = deleteTransactionalMessageCommand;
            _log = log.Create();
        }

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageId id)
        {
            if (_useTransactions)
                throw new DotNetWorkQueueException("Cannot use a transaction without the message context");

            if (id == null || !id.HasValue) return RemoveMessageStatus.NotFound;

            var count = _deleteMessageCommand.Handle(new DeleteMessageCommand((long)id.Id.Value));
            return count > 0 ? RemoveMessageStatus.Removed : RemoveMessageStatus.NotFound;
        }

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageContext context)
        {
            var connection = context.Get(_headers.Connection);

            //if transaction held
            if (connection.Connection == null || connection.Transaction == null) return RemoveMessageStatus.NotFound;

            //delete the message, and then commit the transaction
            var count =_deleteTransactionalMessageCommand.Handle(new DeleteTransactionalMessageCommand((long)context.MessageId.Id.Value, context));

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
            return count > 0 ? RemoveMessageStatus.Removed : RemoveMessageStatus.NotFound;
        }
    }
}
