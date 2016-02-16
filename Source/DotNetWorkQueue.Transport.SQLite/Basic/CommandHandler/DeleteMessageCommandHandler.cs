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
using System.Data;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.SQLite.Basic.Command;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// Deletes a message from a queue
    /// </summary>
    internal class DeleteMessageCommandHandler : ICommandHandlerWithOutput<DeleteMessageCommand, long>
    {
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly IConnectionInformation _connectionInformation;
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly ISqLiteTransactionFactory _transactionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        public DeleteMessageCommandHandler(ISqLiteMessageQueueTransportOptionsFactory options, 
            IConnectionInformation connectionInformation,
            SqLiteCommandStringCache commandCache,
            ISqLiteTransactionFactory transactionFactory)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => commandCache, commandCache);

            _options = new Lazy<SqLiteMessageQueueTransportOptions>(options.Create);
            _connectionInformation = connectionInformation;
            _commandCache = commandCache;
            _transactionFactory = transactionFactory;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "query is ok")]
        public long Handle(DeleteMessageCommand command)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return 0;
            }

            using (var connection = new SQLiteConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    using (var commandSql = connection.CreateCommand())
                    {
                        commandSql.Transaction = trans;

                        //set ID
                        commandSql.Parameters.Add("@QueueID", DbType.Int64);
                        commandSql.Parameters["@QueueID"].Value = command.QueueId;

                        //delete the meta data record
                        commandSql.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.DeleteFromMetaData);
                        var i = commandSql.ExecuteNonQuery();
                        if (i != 1) return 0;

                        //delete the message body
                        commandSql.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.DeleteFromQueue);
                        commandSql.ExecuteNonQuery();

                        //delete any error tracking information
                        commandSql.CommandText =
                            _commandCache.GetCommand(SqLiteCommandStringTypes.DeleteFromErrorTracking);
                        commandSql.ExecuteNonQuery();

                        //delete status record
                        if (!_options.Value.EnableStatusTable)
                        {
                            trans.Commit();
                            return 1;
                        }

                        commandSql.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.DeleteFromStatus);
                        commandSql.ExecuteNonQuery();
                        trans.Commit();
                        return 1;
                    }
                }
            }
        }
    }
}
