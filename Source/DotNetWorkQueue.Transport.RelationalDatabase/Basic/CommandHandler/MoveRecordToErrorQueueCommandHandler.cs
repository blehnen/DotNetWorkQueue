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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Moves a record from the meta table to the error table
    /// </summary>
    public class MoveRecordToErrorQueueCommandHandler<TConnection, TTransaction, TCommand> : ICommandHandler<MoveRecordToErrorQueueCommand>
        where TConnection : class, IDbConnection
        where TTransaction : class, IDbTransaction
        where TCommand : class, IDbCommand
    {
        private readonly ICommandHandler<DeleteMetaDataCommand> _deleteMetaCommandHandler;
        private readonly ICommandHandler<SetStatusTableStatusTransactionCommand> _setStatusCommandHandler;
        private readonly ICommandHandler<SetStatusTableStatusCommand> _setStatusNoTransactionCommandHandler;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPrepareCommandHandler<MoveRecordToErrorQueueCommand> _prepareCommand;
        private readonly Lazy<ITransportOptions> _options;
        private readonly IConnectionHeader<TConnection, TTransaction, TCommand> _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveRecordToErrorQueueCommandHandler{TConnection, TTransaction, TCommand}" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="deleteMetaCommandHandler">The delete meta command handler.</param>
        /// <param name="setStatusCommandHandler">The set status command handler.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="setCommandHandler">The set command handler.</param>
        public MoveRecordToErrorQueueCommandHandler(
            ITransportOptionsFactory options,
            ICommandHandler<DeleteMetaDataCommand> deleteMetaCommandHandler,
            ICommandHandler<SetStatusTableStatusTransactionCommand> setStatusCommandHandler,
            IDbConnectionFactory dbConnectionFactory,
            ITransactionFactory transactionFactory,
            IPrepareCommandHandler<MoveRecordToErrorQueueCommand> prepareCommand,
            IConnectionHeader<TConnection, TTransaction, TCommand> headers,
            ICommandHandler<SetStatusTableStatusCommand> setCommandHandler)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => transactionFactory, transactionFactory);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => prepareCommand, prepareCommand);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => deleteMetaCommandHandler, deleteMetaCommandHandler);
            Guard.NotNull(() => setStatusCommandHandler, setStatusCommandHandler);
            Guard.NotNull(() => setCommandHandler, setCommandHandler);

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
        public void Handle(MoveRecordToErrorQueueCommand command)
        {
            if (_options.Value.EnableHoldTransactionUntilMessageCommitted)
            {
                HandleForTransaction(command);
            }
            else
            {
                using (var conn = _dbConnectionFactory.Create())
                {
                    conn.Open();
                    using (var trans = _transactionFactory.Create(conn).BeginTransaction())
                    {
                        using (var commandSql = conn.CreateCommand())
                        {
                            commandSql.Transaction = trans;
                            _prepareCommand.Handle(command, commandSql, CommandStringTypes.MoveToErrorQueue);
                            var iCount = commandSql.ExecuteNonQuery();
                            if (iCount != 1) return;

                            //the record is now in the error queue, remove it from the main queue
                            _deleteMetaCommandHandler.Handle(new DeleteMetaDataCommand(command.QueueId, conn, trans));

                            if (!_options.Value.EnableStatusTable)
                            {
                                trans.Commit();
                                return;
                            }

                            //update the status record
                            _setStatusCommandHandler.Handle(new SetStatusTableStatusTransactionCommand(command.QueueId, conn, QueueStatuses.Error, trans));
                        }
                        trans.Commit();
                    }
                }
            }
        }

        private void HandleForTransaction(MoveRecordToErrorQueueCommand command)
        {
            var connHolder = command.MessageContext.Get(_headers.Connection);
            using (var conn = _dbConnectionFactory.Create())
            {
                conn.Open();
                using (var commandSql = conn.CreateCommand())
                {
                    _prepareCommand.Handle(command, commandSql, CommandStringTypes.MoveToErrorQueue);
                    var iCount = commandSql.ExecuteNonQuery();
                    if (iCount != 1) return;

                    //the record is now in the error queue, remove it from the main queue
                    _deleteMetaCommandHandler.Handle(new DeleteMetaDataCommand(command.QueueId, connHolder.Connection, connHolder.Transaction));

                    //commit the original transaction
                    connHolder.Transaction.Commit();
                    connHolder.Transaction.Dispose();
                    connHolder.Transaction = null;

                    if (_options.Value.EnableStatusTable)
                    {
                        _setStatusNoTransactionCommandHandler.Handle(new SetStatusTableStatusCommand(command.QueueId, QueueStatuses.Error));
                    }
                }
            }
        }
    }
}
