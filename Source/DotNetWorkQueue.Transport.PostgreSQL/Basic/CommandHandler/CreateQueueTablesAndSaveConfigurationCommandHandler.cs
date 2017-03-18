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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Command;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    /// <summary>
    /// Creates a queue and saves the configuration
    /// </summary>
    internal class CreateQueueTablesAndSaveConfigurationCommandHandler : ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand, QueueCreationResult>
    {
        private readonly IInternalSerializer _serializer;
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;
        private readonly IConnectionInformation _connectionInformation;
        private readonly PostgreSqlCommandStringCache _commandCache;
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateQueueTablesAndSaveConfigurationCommandHandler" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="commandCache">The command cache.</param>
        public CreateQueueTablesAndSaveConfigurationCommandHandler(IPostgreSqlMessageQueueTransportOptionsFactory options, 
            IInternalSerializer serializer, 
            IConnectionInformation connectionInformation,
            PostgreSqlCommandStringCache commandCache)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => commandCache, commandCache);

            _serializer = serializer;
            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(options.Create);
            _connectionInformation = connectionInformation;
            _commandCache = commandCache;
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public QueueCreationResult Handle(CreateQueueTablesAndSaveConfigurationCommand command)
        {
            var script = string.Empty;
            try
            {
                using (var conn = new NpgsqlConnection(_connectionInformation.ConnectionString))
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
            catch (PostgresException error)
            {
                if (error.SqlState == "42P07" || error.SqlState == "42710")
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
        private void SaveConfiguration(NpgsqlConnection conn, NpgsqlTransaction trans)
        {
            using (var commandSql = conn.CreateCommand())
            {
                commandSql.Transaction = trans;
                commandSql.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.SaveConfiguration);
                commandSql.Parameters.Add("@Configuration", NpgsqlDbType.Bytea, -1).Value =
                    _serializer.ConvertToBytes(_options.Value);
                commandSql.ExecuteNonQuery();
            }
        }
    }
}
