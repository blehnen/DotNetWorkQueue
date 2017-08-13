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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// Deletes a message from a queue
    /// </summary>
    internal class DeleteMessageCommandHandler : ICommandHandlerWithOutput<DeleteMessageCommand, long>
    {
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly IConnectionInformation _connectionInformation;
        private readonly SqlServerCommandStringCache _commandCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="commandCache">The command cache.</param>
        public DeleteMessageCommandHandler(ISqlServerMessageQueueTransportOptionsFactory options, 
            IConnectionInformation connectionInformation,
            SqlServerCommandStringCache commandCache)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => commandCache, commandCache);

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
        public long Handle(DeleteMessageCommand command)
        {
            using (var connection = new SqlConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    using (var commandSql = connection.CreateCommand())
                    {
                        commandSql.Transaction = trans;

                        //set ID
                        commandSql.Parameters.Add("@QueueID", SqlDbType.BigInt);
                        commandSql.Parameters["@QueueID"].Value = command.QueueId;

                        //delete the meta data record
                        commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.DeleteFromMetaData);
                        commandSql.ExecuteNonQuery();

                        //delete the message body
                        commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.DeleteFromQueue);
                        commandSql.ExecuteNonQuery();

                        //delete any error tracking information
                        commandSql.CommandText =
                            _commandCache.GetCommand(CommandStringTypes.DeleteFromErrorTracking);
                        commandSql.ExecuteNonQuery();

                        commandSql.CommandText =
                            _commandCache.GetCommand(CommandStringTypes.DeleteFromMetaDataErrors);
                        commandSql.ExecuteNonQuery();

                        //delete status record
                        if (!_options.Value.EnableStatusTable)
                        {
                            trans.Commit();
                            return 1;
                        }

                        commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.DeleteFromStatus);
                        commandSql.ExecuteNonQuery();
                        trans.Commit();
                        return 1;
                    }
                }
            }
        }
    }
}
