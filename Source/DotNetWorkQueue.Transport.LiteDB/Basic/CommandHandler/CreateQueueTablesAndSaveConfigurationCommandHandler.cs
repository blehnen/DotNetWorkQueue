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
using System;
using DotNetWorkQueue.Transport.LiteDb.Basic.Command;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <summary>
    /// Creates a queue and saves the configuration
    /// </summary>
    internal class CreateQueueTablesAndSaveConfigurationCommandHandler : ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult>
    {
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly IInternalSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateQueueTablesAndSaveConfigurationCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="serializer">The serializer.</param>
        public CreateQueueTablesAndSaveConfigurationCommandHandler(LiteDbConnectionManager connectionInformation,
            ILiteDbMessageQueueTransportOptionsFactory optionsFactory,
            TableNameHelper tableNameHelper,
            IInternalSerializer serializer)
        {
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => serializer, serializer);

            _options = new Lazy<LiteDbMessageQueueTransportOptions>(optionsFactory.Create);
            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
        }

        /// <inheritdoc />
        public QueueCreationResult Handle(CreateQueueTablesAndSaveConfigurationCommand<ITable> command)
        {
            //create database and enforce UTC date de-serialization
            using (var db = _connectionInformation.GetDatabase())
            {
                db.Database.Pragma("UTC_DATE", true);


                //create all tables
                foreach (var table in command.Tables)
                {
                    table.Create(_connectionInformation, _options.Value, _tableNameHelper);
                }

                //save configuration
                foreach (var table in command.Tables)
                {
                    if (table is ConfigurationTable configTable)
                    {
                        var col = db.Database.GetCollection<ConfigurationTable>(_tableNameHelper.ConfigurationName);
                        configTable.Configuration = _serializer.ConvertToBytes(_options.Value);
                        col.Insert(configTable);
                        break;
                    }
                }

                return new QueueCreationResult(QueueCreationStatus.Success);
            }
        }
    }
}
