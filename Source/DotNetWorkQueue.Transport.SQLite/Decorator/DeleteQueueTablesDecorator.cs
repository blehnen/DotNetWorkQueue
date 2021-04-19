// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class DeleteQueueTablesDecorator : ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult> _decorated;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteQueueTablesDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        /// <param name="databaseExists">The database exists.</param>
        public DeleteQueueTablesDecorator(IConnectionInformation connectionInformation,
            ICommandHandlerWithOutput<DeleteQueueTablesCommand, QueueRemoveResult> decorated,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => databaseExists, databaseExists);
            _connectionInformation = connectionInformation;
            _decorated = decorated;
            _databaseExists = databaseExists;
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public QueueRemoveResult Handle(DeleteQueueTablesCommand command)
        {
            return !_databaseExists.Exists(_connectionInformation.ConnectionString) ? new QueueRemoveResult(QueueRemoveStatus.DoesNotExist) : _decorated.Handle(command);
        }
    }
}
