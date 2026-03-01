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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Dashboard command: moves an error message back to Waiting status by deleting its error
    /// records and resetting the MetaData status.
    /// </summary>
    internal class DashboardRequeueErrorMessageCommandHandler : ICommandHandlerWithOutput<DashboardRequeueErrorMessageCommand, long>
    {
        private readonly Lazy<ITransportOptions> _options;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPrepareCommandHandler<DashboardRequeueErrorMessageCommand> _prepareCommand;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardRequeueErrorMessageCommandHandler"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public DashboardRequeueErrorMessageCommandHandler(ITransportOptionsFactory options,
            IDbConnectionFactory dbConnectionFactory,
            ITransactionFactory transactionFactory,
            IPrepareCommandHandler<DashboardRequeueErrorMessageCommand> prepareCommand)
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
        public long Handle(DashboardRequeueErrorMessageCommand command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    using (var commandSql = connection.CreateCommand())
                    {
                        commandSql.Transaction = trans;

                        // Remove error tracking records for this message
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DeleteFromMetaDataErrors);
                        commandSql.ExecuteNonQuery();

                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DeleteFromErrorTracking);
                        commandSql.ExecuteNonQuery();

                        // Reset the message status back to Waiting (only if currently in Error state)
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardRequeueErrorMessage);
                        commandSql.ExecuteNonQuery();

                        if (_options.Value.EnableStatusTable)
                        {
                            _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardRequeueStatusTable);
                            commandSql.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return 1;
                    }
                }
            }
        }
    }
}
