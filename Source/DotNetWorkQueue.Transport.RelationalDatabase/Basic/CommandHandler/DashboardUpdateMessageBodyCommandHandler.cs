// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
    /// Dashboard command: overwrites the Body and Headers columns for a single message in the
    /// Queue table. Both columns are always updated together because interceptors (compression,
    /// encryption) write their markers into headers when encoding.
    /// </summary>
    internal class DashboardUpdateMessageBodyCommandHandler : ICommandHandlerWithOutput<DashboardUpdateMessageBodyCommand, long>
    {
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPrepareCommandHandler<DashboardUpdateMessageBodyCommand> _prepareCommand;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardUpdateMessageBodyCommandHandler"/> class.
        /// </summary>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        public DashboardUpdateMessageBodyCommandHandler(
            IDbConnectionFactory dbConnectionFactory,
            ITransactionFactory transactionFactory,
            IPrepareCommandHandler<DashboardUpdateMessageBodyCommand> prepareCommand)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => transactionFactory, transactionFactory);
            Guard.NotNull(() => prepareCommand, prepareCommand);

            _transactionFactory = transactionFactory;
            _prepareCommand = prepareCommand;
            _dbConnectionFactory = dbConnectionFactory;
        }

        /// <inheritdoc />
        public long Handle(DashboardUpdateMessageBodyCommand command)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    using (var commandSql = connection.CreateCommand())
                    {
                        commandSql.Transaction = trans;

                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.DashboardUpdateMessageBody);
                        var result = commandSql.ExecuteNonQuery();

                        trans.Commit();
                        return result;
                    }
                }
            }
        }
    }
}
