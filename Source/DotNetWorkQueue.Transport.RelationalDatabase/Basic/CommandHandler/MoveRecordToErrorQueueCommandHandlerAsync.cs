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
    /// Moves a record from the meta table to the error table
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
    public class MoveRecordToErrorQueueCommandHandlerAsync<TConnection, TTransaction, TCommand> : ICommandHandlerAsync<MoveRecordToErrorQueueCommand<long>>
        where TConnection : DbConnection
        where TTransaction : DbTransaction
        where TCommand : DbCommand
    {
        private readonly ICommandHandlerAsync<DeleteMetaDataCommand> _deleteMetaCommandHandler;
        private readonly ICommandHandlerAsync<SetStatusTableStatusTransactionCommand> _setStatusCommandHandler;
        private readonly ICommandHandlerAsync<SetStatusTableStatusCommand<long>> _setStatusNoTransactionCommandHandler;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPrepareCommandHandler<MoveRecordToErrorQueueCommand<long>> _prepareCommand;
        private readonly Lazy<ITransportOptions> _options;
        private readonly IConnectionHeader<TConnection, TTransaction, TCommand> _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveRecordToErrorQueueCommandHandlerAsync{TConnection, TTransaction, TCommand}" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="deleteMetaCommandHandler">The delete meta command handler.</param>
        /// <param name="setStatusCommandHandler">The set status command handler.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="setCommandHandler">The set command handler.</param>
        public MoveRecordToErrorQueueCommandHandlerAsync(
            ITransportOptionsFactory options,
            ICommandHandlerAsync<DeleteMetaDataCommand> deleteMetaCommandHandler,
            ICommandHandlerAsync<SetStatusTableStatusTransactionCommand> setStatusCommandHandler,
            IDbConnectionFactory dbConnectionFactory,
            ITransactionFactory transactionFactory,
            IPrepareCommandHandler<MoveRecordToErrorQueueCommand<long>> prepareCommand,
            IConnectionHeader<TConnection, TTransaction, TCommand> headers,
            ICommandHandlerAsync<SetStatusTableStatusCommand<long>> setCommandHandler)
        {
            Guard.NotNull(options);
            Guard.NotNull(transactionFactory);
            Guard.NotNull(headers);
            Guard.NotNull(prepareCommand);
            Guard.NotNull(dbConnectionFactory);
            Guard.NotNull(deleteMetaCommandHandler);
            Guard.NotNull(setStatusCommandHandler);
            Guard.NotNull(setCommandHandler);

            _options = new Lazy<ITransportOptions>(options.Create);
            _deleteMetaCommandHandler = deleteMetaCommandHandler;
            _setStatusCommandHandler = setStatusCommandHandler;
            _dbConnectionFactory = dbConnectionFactory;
            _transactionFactory = transactionFactory;
            _prepareCommand = prepareCommand;
            _headers = headers;
            _setStatusNoTransactionCommandHandler = setCommandHandler;
        }
        /// <inheritdoc />
        public async Task HandleAsync(MoveRecordToErrorQueueCommand<long> command)
        {
            if (_options.Value.EnableHoldTransactionUntilMessageCommitted)
            {
                await HandleForTransactionAsync(command).ConfigureAwait(false);
            }
            else
            {
                using (var conn = _dbConnectionFactory.Create())
                {
                    await conn.OpenAsync().ConfigureAwait(false);
                    using (var trans = await _transactionFactory.Create(conn).BeginTransactionAsync().ConfigureAwait(false))
                    {
                        using (var commandSql = conn.CreateCommand())
                        {
                            commandSql.Transaction = trans;
                            _prepareCommand.Handle(command, commandSql, CommandStringTypes.MoveToErrorQueue);
                            var iCount = await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);
                            if (iCount != 1) return;

                            //the record is now in the error queue, remove it from the main queue
                            await _deleteMetaCommandHandler.HandleAsync(new DeleteMetaDataCommand(command.QueueId, conn, trans)).ConfigureAwait(false);

                            if (!_options.Value.EnableStatusTable)
                            {
                                await trans.CommitAsync().ConfigureAwait(false);
                                return;
                            }

                            //update the status record
                            await _setStatusCommandHandler.HandleAsync(new SetStatusTableStatusTransactionCommand(command.QueueId, conn, QueueStatuses.Error, trans)).ConfigureAwait(false);
                        }
                        await trans.CommitAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task HandleForTransactionAsync(MoveRecordToErrorQueueCommand<long> command)
        {
            var connHolder = command.MessageContext.Get(_headers.Connection);

            //see the synchronous handler: the insert shares the held transaction so the move has one
            //outcome, rather than committing the error copy on a connection of its own and leaving it
            //behind when the source delete or commit fails
            using (var commandSql = connHolder.Connection.CreateCommand())
            {
                commandSql.Transaction = connHolder.Transaction;
                _prepareCommand.Handle(command, commandSql, CommandStringTypes.MoveToErrorQueue);
                var iCount = await commandSql.ExecuteNonQueryAsync().ConfigureAwait(false);
                if (iCount != 1) return;

                //the record is now in the error queue, remove it from the main queue
                await _deleteMetaCommandHandler.HandleAsync(new DeleteMetaDataCommand(command.QueueId, connHolder.Connection, connHolder.Transaction)).ConfigureAwait(false);

                //commit the original transaction
                await connHolder.Transaction.CommitAsync().ConfigureAwait(false);
                connHolder.Transaction.Dispose();
                connHolder.Transaction = null;

                if (_options.Value.EnableStatusTable)
                {
                    await _setStatusNoTransactionCommandHandler.HandleAsync(new SetStatusTableStatusCommand<long>(command.QueueId, QueueStatuses.Error)).ConfigureAwait(false);
                }
            }
        }
    }
}
