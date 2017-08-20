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

using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    internal class DeleteQueueTablesCommandHandler : ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>
    {
        private readonly TableNameHelper _tableNameHelper;
        private readonly CommandStringCache _commandCache;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteQueueTablesCommandHandler" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public DeleteQueueTablesCommandHandler(
            TableNameHelper tableNameHelper,
            CommandStringCache commandCache,
            ITransactionFactory transactionFactory,
            IDbConnectionFactory dbConnectionFactory,
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => transactionFactory, transactionFactory);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _tableNameHelper = tableNameHelper;
            _commandCache = commandCache;
            _transactionFactory = transactionFactory;
            _dbConnectionFactory = dbConnectionFactory;
            _connectionInformation = connectionInformation;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="inputCommand">The input command.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "query is ok")]
        public QueueRemoveResult Handle(DeleteQueueTablesCommand inputCommand)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    foreach (var table in _tableNameHelper.Tables)
                    {
                        using (var commandExists = connection.CreateCommand())
                        {
                            commandExists.Transaction = trans;
                            commandExists.CommandText = _commandCache.GetCommand(CommandStringTypes.GetTableExists);

                            var parameter = commandExists.CreateParameter();
                            parameter.ParameterName = "@Table";
                            parameter.DbType = DbType.AnsiString;
                            parameter.Value = table;
                            commandExists.Parameters.Add(parameter);

                            var parameterDb = commandExists.CreateParameter();
                            parameterDb.ParameterName = "@Database";
                            parameterDb.DbType = DbType.AnsiString;
                            parameterDb.Value = _connectionInformation.Container;
                            commandExists.Parameters.Add(parameterDb);

                            bool delete;
                            using (var reader = commandExists.ExecuteReader())
                            {
                                delete = reader.Read();
                            }
                            if (!delete) continue;
                            using (var commandSql = connection.CreateCommand())
                            {
                                commandSql.Transaction = trans;
                                commandSql.CommandText =
                                    _commandCache.GetCommand(CommandStringTypes.DeleteTable, table);
                                commandSql.ExecuteNonQuery();
                            }
                        }
                    }
                    trans.Commit();
                    return new QueueRemoveResult(QueueRemoveStatus.Success);
                }
            }
        }
    }
}
