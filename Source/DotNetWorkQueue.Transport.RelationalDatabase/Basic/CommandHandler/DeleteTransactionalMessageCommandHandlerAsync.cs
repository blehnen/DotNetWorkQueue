// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Data.Common;
using System.Threading.Tasks;
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
    /// <remarks>
    /// <para>A retry of this handler cannot succeed while the caller holds the transaction
    /// (<c>EnableHoldTransactionUntilMessageCommitted</c>). PostgreSQL aborts a transaction as soon
    /// as a statement in it fails, so every later statement returns <c>25P02</c>,
    /// <c>in_failed_sql_transaction</c>; the work has to be redone on a new transaction, which only
    /// the caller can open.</para>
    /// <para>It is left wrapped in the retry decorator on purpose. The cost is bounded to a single
    /// attempt: <c>25P02</c> is not one of the retryable states, and the pipeline's predicate runs
    /// after every attempt, so the second failure propagates instead of backing off again. One
    /// wasted retry of about half a second, on a path that is already failing, does not justify
    /// teaching the decorator which commands run inside someone else's transaction. See GitHub
    /// issue #309 for the measurement and the options that were weighed.</para>
    /// <para>Any new handler that runs inside the caller's transaction inherits this, and nothing
    /// will report it.</para>
    /// </remarks>
    public class DeleteTransactionalMessageCommandHandlerAsync<TConnection, TTransaction, TCommand> : ICommandHandlerWithOutputAsync<DeleteTransactionalMessageCommand, long>
        where TConnection : DbConnection
        where TTransaction : DbTransaction
        where TCommand : DbCommand
    {
        private readonly Lazy<ITransportOptions> _options;
        private readonly IConnectionHeader<TConnection, TTransaction, TCommand> _headers;
        private readonly IPrepareCommandHandler<DeleteMessageCommand<long>> _prepareCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteTransactionalMessageCommandHandlerAsync{TConnection, TTransaction, TCommand}"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public DeleteTransactionalMessageCommandHandlerAsync(ITransportOptionsFactory options,
            IConnectionHeader<TConnection, TTransaction, TCommand> headers,
            IPrepareCommandHandler<DeleteMessageCommand<long>> prepareCommand)
        {
            Guard.NotNull(options);
            Guard.NotNull(headers);
            Guard.NotNull(prepareCommand);

            _options = new Lazy<ITransportOptions>(options.Create);
            _headers = headers;
            _prepareCommand = prepareCommand;
        }

        /// <inheritdoc />
        public async Task<long> HandleAsync(DeleteTransactionalMessageCommand command)
        {
            var connection = command.MessageContext.Get(_headers.Connection);
            using (var commandSql = connection.CreateCommand())
            {
                //delete the meta data record
                _prepareCommand.Handle(new DeleteMessageCommand<long>(command.QueueId), commandSql, CommandStringTypes.DeleteFromMetaData);
                await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);

                //The queue row is what says the message was there; the other rows are derived from it
                _prepareCommand.Handle(new DeleteMessageCommand<long>(command.QueueId), commandSql, CommandStringTypes.DeleteFromQueue);
                var removed = await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);

                //delete any error tracking information
                _prepareCommand.Handle(new DeleteMessageCommand<long>(command.QueueId), commandSql, CommandStringTypes.DeleteFromErrorTracking);
                await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);

                _prepareCommand.Handle(new DeleteMessageCommand<long>(command.QueueId), commandSql, CommandStringTypes.DeleteFromMetaDataErrors);
                await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);

                //delete status record
                if (!_options.Value.EnableStatusTable) return removed;

                _prepareCommand.Handle(new DeleteMessageCommand<long>(command.QueueId), commandSql, CommandStringTypes.DeleteFromStatus);
                await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);
                return removed;
            }
        }
    }
}
