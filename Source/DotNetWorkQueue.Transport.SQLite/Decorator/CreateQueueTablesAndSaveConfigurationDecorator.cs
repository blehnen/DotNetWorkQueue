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
using System.Data.SQLite;
using System.IO;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    internal class CreateQueueTablesAndSaveConfigurationDecorator : ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult> _decorated;
        private readonly IGetFileNameFromConnectionString _getFileNameFromConnection;
        private readonly DatabaseExists _databaseExists;

        public CreateQueueTablesAndSaveConfigurationDecorator(IConnectionInformation connectionInformation,
            ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult> decorated,
            IGetFileNameFromConnectionString getFileNameFromConnection,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => getFileNameFromConnection, getFileNameFromConnection);
            Guard.NotNull(() => databaseExists, databaseExists);

            _connectionInformation = connectionInformation;
            _decorated = decorated;
            _getFileNameFromConnection = getFileNameFromConnection;
            _databaseExists = databaseExists;
        }
        public QueueCreationResult Handle(CreateQueueTablesAndSaveConfigurationCommand<ITable> command)
        {
            if (!_databaseExists.Exists(_connectionInformation.ConnectionString))
            { //no db file, create
                var fileName = _getFileNameFromConnection.GetFileName(_connectionInformation.ConnectionString);
                using (File.Create(fileName.FileName))
                {
                }
            }

            try
            {
                return _decorated.Handle(command);
            }
            //if the queue already exists, return that status; otherwise, bubble the error
            catch (SQLiteException error)
            {
                if (error.ResultCode == SQLiteErrorCode.Error && error.Message.Contains("table") && error.Message.Contains("already exists"))
                {
                    return new QueueCreationResult(QueueCreationStatus.AttemptedToCreateAlreadyExists);
                }
                throw new DotNetWorkQueueException("Failed to create table",
                    error);
            }
        }
    }
}
