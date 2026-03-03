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
    /// Dashboard command: deletes all error messages from all queue tables.
    /// </summary>
    internal class DashboardDeleteAllErrorMessagesCommandHandler : ICommandHandlerWithOutput<DashboardDeleteAllErrorMessagesCommand, long>
    {
        private readonly Lazy<ITransportOptions> _options;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPrepareCommandHandler<DashboardDeleteAllErrorMessagesCommand> _prepareCommand;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardDeleteAllErrorMessagesCommandHandler"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public DashboardDeleteAllErrorMessagesCommandHandler(ITransportOptionsFactory options,
            IDbConnectionFactory dbConnectionFactory,
            ITransactionFactory transactionFactory,
            IPrepareCommandHandler<DashboardDeleteAllErrorMessagesCommand> prepareCommand)
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
        public long Handle(DashboardDeleteAllErrorMessagesCommand command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    using (var commandSql = connection.CreateCommand())
                    {
                        commandSql.Transaction = trans;

                        // Delete dependent tables first (they reference MetaDataErrors for QueueIDs)
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardDeleteAllErrors_ErrorTracking);
                        commandSql.ExecuteNonQuery();

                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardDeleteAllErrors_Queue);
                        commandSql.ExecuteNonQuery();

                        // Safety: also remove any MetaData records that may still exist
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardDeleteAllErrors_MetaData);
                        commandSql.ExecuteNonQuery();

                        if (_options.Value.EnableStatusTable)
                        {
                            _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardDeleteAllErrors_Status);
                            commandSql.ExecuteNonQuery();
                        }

                        // Delete MetaDataErrors last (its QueueIDs are used by the sub-queries above)
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardDeleteAllErrors_MetaDataErrors);
                        var count = commandSql.ExecuteNonQuery();

                        trans.Commit();
                        return count;
                    }
                }
            }
        }
    }
}
