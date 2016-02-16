// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic.Command;
namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    internal class DeleteQueueTablesCommandHandler : ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly ISqLiteTransactionFactory _transactionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteQueueTablesCommandHandler" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        public DeleteQueueTablesCommandHandler(IConnectionInformation connectionInformation,
            TableNameHelper tableNameHelper,
            SqLiteCommandStringCache commandCache,
            ISqLiteTransactionFactory transactionFactory)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _commandCache = commandCache;
            _transactionFactory = transactionFactory;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "query is ok")]
        public QueueRemoveResult Handle(DeleteQueueTablesCommand command)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
            }

            using (var connection = new SQLiteConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    foreach (var table in _tableNameHelper.Tables)
                    {
                        using (var commandExists = connection.CreateCommand())
                        {
                            commandExists.Transaction = trans;
                            commandExists.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.GetTableExists);
                            commandExists.Parameters.AddWithValue("@Table", table);
                            bool delete;
                            using (var reader = commandExists.ExecuteReader())
                            {
                                delete = reader.Read();
                            }
                            if (!delete) continue;
                            using (var commandSql = connection.CreateCommand())
                            {
                                commandSql.Transaction = trans;
                                commandSql.CommandText = $"drop table {table}";
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
