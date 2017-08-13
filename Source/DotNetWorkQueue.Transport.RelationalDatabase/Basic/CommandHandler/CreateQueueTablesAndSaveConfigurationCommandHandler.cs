// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <summary>
    /// Creates a queue and saves the configuration
    /// </summary>
    internal class CreateQueueTablesAndSaveConfigurationCommandHandler : ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<ITable>, QueueCreationResult>
    {
        private readonly IOptionsSerialization _options;
        private readonly CommandStringCache _commandCache;
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateQueueTablesAndSaveConfigurationCommandHandler" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        public CreateQueueTablesAndSaveConfigurationCommandHandler(IOptionsSerialization options, 
            CommandStringCache commandCache,
            IDbConnectionFactory connectionFactory)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionFactory, connectionFactory);

            _options = options;
            _commandCache = commandCache;
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification =
            "Query checked")]
        public QueueCreationResult Handle(CreateQueueTablesAndSaveConfigurationCommand<ITable> command)
        {
            using (var conn = _connectionFactory.Create())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    foreach (var t in command.Tables)
                    {
                        using (var commandSql = conn.CreateCommand())
                        {
                            commandSql.Transaction = trans;
                            commandSql.CommandText = t.Script();
                            commandSql.ExecuteNonQuery();
                        }
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
                commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.SaveConfiguration);
                var param = commandSql.CreateParameter();
                param.ParameterName = "@Configuration";
                param.DbType = DbType.Binary;
                param.Value = _options.ConvertToBytes();
                commandSql.Parameters.Add(param);
                commandSql.ExecuteNonQuery();
            }
        }
    }
}
