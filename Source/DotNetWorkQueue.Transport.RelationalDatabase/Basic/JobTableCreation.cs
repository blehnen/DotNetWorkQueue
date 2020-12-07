// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <inheritdoc />
    public class JobTableCreation: IJobTableCreation
    {
        private readonly IJobSchema _createSchema;
        private readonly IQueryHandler<GetTableExistsQuery, bool> _queryTableExists;
        private readonly IConnectionInformation _connection;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult>
            _createCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobTableCreation" /> class.
        /// </summary>
        /// <param name="queryTableExists">The query table exists.</param>
        /// <param name="createSchema">The create schema.</param>
        /// <param name="createCommand">The create command.</param>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public JobTableCreation(IQueryHandler<GetTableExistsQuery, bool> queryTableExists,
            IJobSchema createSchema,
            ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult> createCommand,
            IConnectionInformation connectionInfo,
            ITableNameHelper tableNameHelper
            )
        {
            Guard.NotNull(() => createSchema, createSchema);
            Guard.NotNull(() => queryTableExists, queryTableExists);
            Guard.NotNull(() => createCommand, createCommand);
            Guard.NotNull(() => connectionInfo, connectionInfo);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _createSchema = createSchema;
            _queryTableExists = queryTableExists;
            _createCommand = createCommand;
            _connection = connectionInfo;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public bool JobTableExists => _queryTableExists.Handle(new GetTableExistsQuery(_connection.ConnectionString,
            _tableNameHelper.JobTableName));

        /// <inheritdoc />
        public QueueCreationResult CreateJobTable()
        {
            return !JobTableExists ? CreateTable() : new QueueCreationResult(QueueCreationStatus.AlreadyExists);
        }

        /// <summary>
        /// Creates the queue.
        /// </summary>
        /// <returns></returns>
        private QueueCreationResult CreateTable()
        {
            return
                _createCommand.Handle(
                    new CreateJobTablesCommand<ITable>(_createSchema.GetSchema()));
        }
    }
}
