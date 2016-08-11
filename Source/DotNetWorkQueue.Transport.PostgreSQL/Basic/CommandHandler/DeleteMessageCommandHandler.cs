// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Command;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    /// <summary>
    /// Deletes a message from a queue
    /// </summary>
    internal class DeleteMessageCommandHandler : ICommandHandlerWithOutput<DeleteMessageCommand, long>
    {
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;
        private readonly IConnectionInformation _connectionInformation;
        private readonly PostgreSqlCommandStringCache _commandCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="commandCache">The command cache.</param>
        public DeleteMessageCommandHandler(IPostgreSqlMessageQueueTransportOptionsFactory options, 
            IConnectionInformation connectionInformation,
            PostgreSqlCommandStringCache commandCache)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => commandCache, commandCache);

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
        public long Handle(DeleteMessageCommand command)
        {
            using (var connection = new NpgsqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    using (var commandSql = connection.CreateCommand())
                    {
                        commandSql.Transaction = trans;

                        //set ID
                        commandSql.Parameters.Add("@QueueID", NpgsqlDbType.Bigint);
                        commandSql.Parameters["@QueueID"].Value = command.QueueId;

                        //delete the meta data record
                        commandSql.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.DeleteFromMetaData);
                        var i = commandSql.ExecuteNonQuery();
                        if (i != 1) return 0;

                        //delete the message body
                        commandSql.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.DeleteFromQueue);
                        commandSql.ExecuteNonQuery();

                        //delete any error tracking information
                        commandSql.CommandText =
                            _commandCache.GetCommand(PostgreSqlCommandStringTypes.DeleteFromErrorTracking);
                        commandSql.ExecuteNonQuery();

                        //delete status record
                        if (!_options.Value.EnableStatusTable)
                        {
                            trans.Commit();
                            return 1;
                        }

                        commandSql.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.DeleteFromStatus);
                        commandSql.ExecuteNonQuery();
                        trans.Commit();
                        return 1;
                    }
                }
            }
        }
    }
}
