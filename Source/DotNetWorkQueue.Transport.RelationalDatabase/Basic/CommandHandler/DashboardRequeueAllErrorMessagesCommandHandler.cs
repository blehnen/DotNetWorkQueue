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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Dashboard command: requeues all error messages back to Waiting status.
    /// Re-creates MetaData rows, clears error tracking, and deletes MetaDataErrors.
    /// </summary>
    internal class DashboardRequeueAllErrorMessagesCommandHandler : ICommandHandlerWithOutput<DashboardRequeueAllErrorMessagesCommand, long>
    {
        private readonly Lazy<ITransportOptions> _options;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPrepareCommandHandler<DashboardRequeueAllErrorMessagesCommand> _prepareCommand;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardRequeueAllErrorMessagesCommandHandler"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public DashboardRequeueAllErrorMessagesCommandHandler(ITransportOptionsFactory options,
            IDbConnectionFactory dbConnectionFactory,
            ITransactionFactory transactionFactory,
            IPrepareCommandHandler<DashboardRequeueAllErrorMessagesCommand> prepareCommand)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => transactionFactory, transactionFactory);
            Guard.NotNull(() => prepareCommand, prepareCommand);

            _options = new Lazy<ITransportOptions>(options.Create);
            _transactionFactory = transactionFactory;
            _prepareCommand = prepareCommand;
            _dbConnectionFactory = dbConnectionFactory;
        }

        /// <inheritdoc />
        public long Handle(DashboardRequeueAllErrorMessagesCommand command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    using (var commandSql = connection.CreateCommand())
                    {
                        commandSql.Transaction = trans;

                        // Re-create MetaData records for all error messages (INSERT from MetaDataErrors)
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardRequeueAllErrors);
                        var count = commandSql.ExecuteNonQuery();

                        // Delete error tracking records (references MetaDataErrors for QueueIDs)
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardRequeueAllErrors_ErrorTracking);
                        commandSql.ExecuteNonQuery();

                        // Update status table before deleting MetaDataErrors (SQL references it for QueueIDs)
                        if (_options.Value.EnableStatusTable)
                        {
                            _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardRequeueAllErrors_StatusTable);
                            commandSql.ExecuteNonQuery();
                        }

                        // Delete MetaDataErrors records last (referenced by queries above)
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardRequeueAllErrors_MetaDataErrors);
                        commandSql.ExecuteNonQuery();

                        trans.Commit();
                        return count;
                    }
                }
            }
        }
    }
}
