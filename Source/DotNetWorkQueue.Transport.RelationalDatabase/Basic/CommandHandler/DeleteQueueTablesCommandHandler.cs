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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    internal class DeleteQueueTablesCommandHandler : ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>
    {
        private readonly TableNameHelper _tableNameHelper;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IPrepareCommandHandler<DeleteTableCommand> _prepareDeleteTable;
        private readonly IQueryHandler<GetTableExistsTransactionQuery, bool> _tableExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteQueueTablesCommandHandler" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="prepareDeleteTable">The prepare delete table.</param>
        /// <param name="tableExists">The table exists.</param>
        public DeleteQueueTablesCommandHandler(
            TableNameHelper tableNameHelper,
            ITransactionFactory transactionFactory,
            IDbConnectionFactory dbConnectionFactory,
            IPrepareCommandHandler<DeleteTableCommand> prepareDeleteTable,
            IQueryHandler<GetTableExistsTransactionQuery, bool> tableExists)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => transactionFactory, transactionFactory);
            Guard.NotNull(() => prepareDeleteTable, prepareDeleteTable);
            Guard.NotNull(() => tableExists, tableExists);

            _tableNameHelper = tableNameHelper;
            _transactionFactory = transactionFactory;
            _dbConnectionFactory = dbConnectionFactory;
            _prepareDeleteTable = prepareDeleteTable;
            _tableExists = tableExists;
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "query is ok")]
        public QueueRemoveResult Handle(DeleteQueueTablesCommand inputCommand)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    foreach (var table in _tableNameHelper.Tables)
                    {
                        var delete =
                            _tableExists.Handle(
                                new GetTableExistsTransactionQuery(connection, trans, table));

                        if (!delete) continue;

                        using (var commandSql = connection.CreateCommand())
                        {
                            commandSql.Transaction = trans;
                            _prepareDeleteTable.Handle(new DeleteTableCommand(table), commandSql,
                                CommandStringTypes.DeleteTable);
                            commandSql.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                    return new QueueRemoveResult(QueueRemoveStatus.Success);
                }
            }
        }
    }
}
