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
using System;
using DotNetWorkQueue.Transport.LiteDb.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <summary>
    /// Creates tables for storing job info
    /// </summary>
    public class CreateJobTablesCommandHandler : ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult>
    {
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly LiteDbConnectionManager _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobTablesCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        public CreateJobTablesCommandHandler(LiteDbConnectionManager connectionInformation,
            ILiteDbMessageQueueTransportOptionsFactory optionsFactory,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _options = new Lazy<LiteDbMessageQueueTransportOptions>(optionsFactory.Create);
            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public QueueCreationResult Handle(CreateJobTablesCommand<ITable> command)
        {
            foreach (var table in command.Tables)
            {
                table.Create(_connectionInformation, _options.Value, _tableNameHelper);
            }
            return new QueueCreationResult(QueueCreationStatus.Success);
        }
    }
}
