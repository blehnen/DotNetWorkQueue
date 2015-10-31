// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using System.Data.SqlClient;
using System.Linq;
using DotNetWorkQueue.Transport.SqlServer.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Basic.Query;
namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    internal class DeleteQueueTablesCommandHandler : ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<GetTableExistsQuery, bool> _tableExists;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteQueueTablesCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="tableExists">The table exists.</param>
        public DeleteQueueTablesCommandHandler(IConnectionInformation connectionInformation, 
            TableNameHelper tableNameHelper, 
            IQueryHandler<GetTableExistsQuery, bool> tableExists)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableExists, tableExists);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _tableExists = tableExists;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public QueueRemoveResult Handle(DeleteQueueTablesCommand command)
        {
            using (var connection = new SqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    using (var commandSql = connection.CreateCommand())
                    {
                        commandSql.Transaction = trans;
                        //delete the tables
                        foreach (var table in _tableNameHelper.Tables.Where(table => _tableExists.Handle(new GetTableExistsQuery(_connectionInformation.ConnectionString,
                            table))))
                        {
                            commandSql.CommandText = $"drop table {table}";
                            commandSql.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return new QueueRemoveResult(QueueRemoveStatus.Success);
                    }
                }
            }
        }
    }
}
