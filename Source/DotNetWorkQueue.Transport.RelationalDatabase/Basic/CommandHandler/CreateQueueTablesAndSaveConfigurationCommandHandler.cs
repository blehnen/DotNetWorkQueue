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
using System.Data;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Creates a queue and saves the configuration
    /// </summary>
    internal class CreateQueueTablesAndSaveConfigurationCommandHandler : ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult>
    {
        private readonly IOptionsSerialization _options;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IPrepareCommandHandler<CreateQueueTablesAndSaveConfigurationCommand<ITable>> _prepareCommand;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPrepareCommandHandler<SaveQueueConfigurationCommand> _prepareSaveConfigurationCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateQueueTablesAndSaveConfigurationCommandHandler" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="prepareCommand">The prepare command.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        /// <param name="prepareSaveConfigurationCommand">The prepare save configuration command.</param>
        public CreateQueueTablesAndSaveConfigurationCommandHandler(IOptionsSerialization options, 
            IDbConnectionFactory connectionFactory,
            IPrepareCommandHandler<CreateQueueTablesAndSaveConfigurationCommand<ITable>> prepareCommand,
            ITransactionFactory transactionFactory,
            IPrepareCommandHandler<SaveQueueConfigurationCommand> prepareSaveConfigurationCommand)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => connectionFactory, connectionFactory);
            Guard.NotNull(() => prepareCommand, prepareCommand);
            Guard.NotNull(() => transactionFactory, transactionFactory);
            Guard.NotNull(() => prepareSaveConfigurationCommand, prepareSaveConfigurationCommand);

            _options = options;
            _connectionFactory = connectionFactory;
            _prepareCommand = prepareCommand;
            _transactionFactory = transactionFactory;
            _prepareSaveConfigurationCommand = prepareSaveConfigurationCommand;
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification =
            "Query checked")]
        public QueueCreationResult Handle(CreateQueueTablesAndSaveConfigurationCommand<ITable> command)
        {
            using (var conn = _connectionFactory.Create())
            {
                conn.Open();
                using (var trans = _transactionFactory.Create(conn).BeginTransaction())
                {
                    using (var commandSql = conn.CreateCommand())
                    {
                        commandSql.Transaction = trans;
                        _prepareCommand.Handle(command, commandSql, CommandStringTypes.CreateQueueTables);
                        commandSql.ExecuteNonQuery();
                    }

                    //save the configuration
                    SaveConfiguration(conn, trans);
                    trans.Commit();
                }
            }
            return new QueueCreationResult(QueueCreationStatus.Success);
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        /// <param name="conn">The connection.</param>
        /// <param name="trans">The trans.</param>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        private void SaveConfiguration(IDbConnection conn, IDbTransaction trans)
        {
            using (var commandSql = conn.CreateCommand())
            {
                commandSql.Transaction = trans;
                _prepareSaveConfigurationCommand.Handle(new SaveQueueConfigurationCommand(_options.ConvertToBytes()), commandSql, CommandStringTypes.SaveConfiguration);
                commandSql.ExecuteNonQuery();
            }
        }
    }
}
