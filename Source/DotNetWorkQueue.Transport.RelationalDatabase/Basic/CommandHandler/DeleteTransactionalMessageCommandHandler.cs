// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Deletes a transactional message from the queue
    /// </summary>
    public class DeleteTransactionalMessageCommandHandler<TConnection, TTransaction, TCommand> : ICommandHandlerWithOutput<DeleteTransactionalMessageCommand, long>
        where TConnection : IDbConnection
        where TTransaction : IDbTransaction
        where TCommand : IDbCommand
    {
        private readonly Lazy<ITransportOptions> _options;
        private readonly IConnectionHeader<TConnection, TTransaction, TCommand> _headers;
        private readonly IPrepareCommandHandler<DeleteMessageCommand<long>> _prepareCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteTransactionalMessageCommandHandler{TConnection, TTransaction, TCommand}"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public DeleteTransactionalMessageCommandHandler(ITransportOptionsFactory options,
            IConnectionHeader<TConnection, TTransaction, TCommand> headers,
            IPrepareCommandHandler<DeleteMessageCommand<long>> prepareCommand)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => prepareCommand, prepareCommand);

            _options = new Lazy<ITransportOptions>(options.Create);
            _headers = headers;
            _prepareCommand = prepareCommand;
        }

        /// <inheritdoc />
        public long Handle(DeleteTransactionalMessageCommand command)
        {
            var connection = command.MessageContext.Get(_headers.Connection);
            using (var commandSql = connection.CreateCommand())
            {
                //delete the meta data record
                _prepareCommand.Handle(new DeleteMessageCommand<long>(command.QueueId), commandSql, CommandStringTypes.DeleteFromMetaData);
                commandSql.ExecuteNonQuery();

                //delete the message body
                _prepareCommand.Handle(new DeleteMessageCommand<long>(command.QueueId), commandSql, CommandStringTypes.DeleteFromQueue);
                commandSql.ExecuteNonQuery();

                //delete any error tracking information
                _prepareCommand.Handle(new DeleteMessageCommand<long>(command.QueueId), commandSql, CommandStringTypes.DeleteFromErrorTracking);
                commandSql.ExecuteNonQuery();

                //delete status record
                if (!_options.Value.EnableStatusTable) return 1;

                _prepareCommand.Handle(new DeleteMessageCommand<long>(command.QueueId), commandSql, CommandStringTypes.DeleteFromStatus);
                commandSql.ExecuteNonQuery();
                return 1;
            }
        }
    }
}
