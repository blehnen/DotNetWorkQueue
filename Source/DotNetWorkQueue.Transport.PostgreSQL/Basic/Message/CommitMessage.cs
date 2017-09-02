// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Message
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
        private readonly IConnectionHeader<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="deleteStatusCommandHandler">The delete status command handler.</param>
        /// <param name="deleteMessageCommand">The delete message command.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="deleteTransactionalMessageCommand">The delete transactional message command.</param>
        public CommitMessage(QueueConsumerConfiguration configuration, 
            ICommandHandler<DeleteStatusTableStatusCommand> deleteStatusCommandHandler,
            ICommandHandlerWithOutput<DeleteMessageCommand, long> deleteMessageCommand,
            IConnectionHeader<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> headers, 
            ICommandHandlerWithOutput<DeleteTransactionalMessageCommand, long> deleteTransactionalMessageCommand)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => deleteStatusCommandHandler, deleteStatusCommandHandler);
            Guard.NotNull(() => deleteMessageCommand, deleteMessageCommand);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => deleteTransactionalMessageCommand, deleteTransactionalMessageCommand);

            _configuration = configuration;
            _deleteStatusCommandHandler = deleteStatusCommandHandler;
            _deleteMessageCommand = deleteMessageCommand;
            _headers = headers;
            _deleteTransactionalMessageCommand = deleteTransactionalMessageCommand;
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
            connection.Transaction.Commit();
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
