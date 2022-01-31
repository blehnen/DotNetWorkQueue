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
using System.Linq;
using DotNetWorkQueue.Transport.LiteDb.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <summary>
    /// Deletes the queue tables from the database
    /// </summary>
    internal class DeleteQueueTablesCommandHandler : ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteQueueTablesCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public DeleteQueueTablesCommandHandler(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public QueueRemoveResult Handle(DeleteQueueTablesCommand inputCommand)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                var dbs = _tableNameHelper.Tables;
                var delete = db.Database.GetCollectionNames().Where(database => dbs.Contains(database)).ToList();
                foreach (var toDelete in delete)
                {
                    db.Database.DropCollection(toDelete);
                }

                return new QueueRemoveResult(QueueRemoveStatus.Success);
            }
        }
    }
}
