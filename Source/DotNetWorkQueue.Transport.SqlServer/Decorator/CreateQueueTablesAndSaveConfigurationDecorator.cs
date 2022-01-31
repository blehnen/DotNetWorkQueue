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
using System.Data.SqlClient;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Decorator
{
    internal class CreateQueueTablesAndSaveConfigurationDecorator : ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult>
    {
        private readonly ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult> _decorated;

        public CreateQueueTablesAndSaveConfigurationDecorator(ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            _decorated = decorated;
        }
        public QueueCreationResult Handle(CreateQueueTablesAndSaveConfigurationCommand<ITable> command)
        {
            try
            {
                return _decorated.Handle(command);
            }
            //if the queue already exists, return that status; otherwise, bubble the error
            catch (SqlException error)
            {
                if (error.Number == 2714)
                {
                    return new QueueCreationResult(QueueCreationStatus.AttemptedToCreateAlreadyExists);
                }
                throw new DotNetWorkQueueException("Failed to create queue",
                    error);
            }
        }
    }
}
