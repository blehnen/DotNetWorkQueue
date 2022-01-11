﻿// ---------------------------------------------------------------------
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
using System.Data.SqlClient;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Basic.Message;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Removes a message from storage
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IRemoveMessage" />
    public class RemoveMessage: IRemoveMessage
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICommandHandler<DeleteStatusTableStatusCommand<long>> _deleteStatusCommandHandler;
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand<long>, long> _deleteMessageCommand;
        private readonly ICommandHandlerWithOutput<DeleteTransactionalMessageCommand, long> _deleteTransactionalMessageCommand;
        private readonly IConnectionHeader<SqlConnection, SqlTransaction, SqlCommand> _headers;
        private readonly ILogger _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="deleteStatusCommandHandler">The delete status command handler.</param>
        /// <param name="deleteMessageCommand">The delete message command.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="deleteTransactionalMessageCommand">The delete transactional message command.</param>
        /// <param name="log">The log.</param>
        public RemoveMessage(QueueConsumerConfiguration configuration,
            ICommandHandler<DeleteStatusTableStatusCommand<long>> deleteStatusCommandHandler,
            ICommandHandlerWithOutput<DeleteMessageCommand<long>, long> deleteMessageCommand,
            IConnectionHeader<SqlConnection, SqlTransaction, SqlCommand> headers,
            ICommandHandlerWithOutput<DeleteTransactionalMessageCommand, long> deleteTransactionalMessageCommand,
            ILogger log)
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
            _log = log;
        }

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageId id, RemoveMessageReason reason)
        {
            if (_configuration.Options().EnableHoldTransactionUntilMessageCommitted && reason == RemoveMessageReason.Complete)
                throw new DotNetWorkQueueException("Cannot use a transaction without the message context");

            if (id == null || !id.HasValue) return RemoveMessageStatus.NotFound;

            var count = _deleteMessageCommand.Handle(new DeleteMessageCommand<long>((long)id.Id.Value));
            return count > 0 ? RemoveMessageStatus.Removed : RemoveMessageStatus.NotFound;
        }

        /// <inheritdoc />
        public RemoveMessageStatus Remove(IMessageContext context, RemoveMessageReason reason)
        {
            if (!_configuration.Options().EnableHoldTransactionUntilMessageCommitted)
                return Remove(context.MessageId, reason);

            var connection = context.Get(_headers.Connection);

            //if transaction held
            if (connection.Connection == null || connection.Transaction == null)
            {
                var counter = _deleteMessageCommand.Handle(new DeleteMessageCommand<long>((long)context.MessageId.Id.Value));
                return counter > 0 ? RemoveMessageStatus.Removed : RemoveMessageStatus.NotFound;
            }

            //delete the message, and then commit the transaction
            var count =_deleteTransactionalMessageCommand.Handle(new DeleteTransactionalMessageCommand((long)context.MessageId.Id.Value, context));

            try
            {
                connection.Transaction.Commit();
            }
            catch (Exception e)
            {
                _log.LogError($"Failed to commit a transaction; this might be due to a DB timeout{System.Environment.NewLine}{e}");

                //don't attempt to use the transaction again at this point.
                connection.Transaction = null;

                throw;
            }

            //ensure that transaction won't be used anymore
            connection.Transaction.Dispose();
            connection.Transaction = null;

            if (_configuration.Options().EnableStatusTable)
            {
                _deleteStatusCommandHandler.Handle(new DeleteStatusTableStatusCommand<long>((long)context.MessageId.Id.Value));
            }
            return count > 0 ? RemoveMessageStatus.Removed : RemoveMessageStatus.NotFound;
        }
    }
}
