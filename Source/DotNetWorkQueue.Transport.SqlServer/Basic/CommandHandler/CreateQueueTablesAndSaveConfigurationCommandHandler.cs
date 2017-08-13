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
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// Creates a queue and saves the configuration
    /// </summary>
    internal class CreateQueueTablesAndSaveConfigurationCommandHandler : ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand<Table>, QueueCreationResult>
    {
        private readonly IInternalSerializer _serializer;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly IConnectionInformation _connectionInformation;
        private readonly SqlServerCommandStringCache _commandCache;
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateQueueTablesAndSaveConfigurationCommandHandler" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="commandCache">The command cache.</param>
        public CreateQueueTablesAndSaveConfigurationCommandHandler(ISqlServerMessageQueueTransportOptionsFactory options, 
            IInternalSerializer serializer, 
            IConnectionInformation connectionInformation,
            SqlServerCommandStringCache commandCache)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => commandCache, commandCache);

            _serializer = serializer;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(options.Create);
            _connectionInformation = connectionInformation;
            _commandCache = commandCache;
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public QueueCreationResult Handle(CreateQueueTablesAndSaveConfigurationCommand<Table> command)
        {
            var script = string.Empty;
            try
            {
                using (var conn = new SqlConnection(_connectionInformation.ConnectionString))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        foreach (var t in command.Tables)
                        {
                            using (var commandSql = conn.CreateCommand())
                            {
                                script = t.Script();
                                commandSql.Transaction = trans;
                                commandSql.CommandText = script;
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
            //if the queue already exists, return that status; otherwise, bubble the error
            catch (SqlException error)
            {
                if (error.Number == 2714)
                {
                    return new QueueCreationResult(QueueCreationStatus.AttemptedToCreateAlreadyExists);
                }
                throw new DotNetWorkQueueException($"Failed to create queue. SQL script was {script}",
                    error);
            }
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        /// <param name="conn">The connection.</param>
        /// <param name="trans">The trans.</param>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        private void SaveConfiguration(SqlConnection conn, SqlTransaction trans)
        {
            using (var commandSql = conn.CreateCommand())
            {
                commandSql.Transaction = trans;
                commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.SaveConfiguration);
                commandSql.Parameters.Add("@Configuration", SqlDbType.VarBinary, -1).Value =
                    _serializer.ConvertToBytes(_options.Value);
                commandSql.ExecuteNonQuery();
            }
        }
    }
}
