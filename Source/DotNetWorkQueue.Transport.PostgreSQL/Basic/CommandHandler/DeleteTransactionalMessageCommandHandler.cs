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
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Command;
using DotNetWorkQueue.Validation;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    /// <summary>
    /// Deletes a transactional message from the queue
    /// </summary>
    internal class DeleteTransactionalMessageCommandHandler : ICommandHandlerWithOutput<DeleteTransactionalMessageCommand, long>
    {
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;
        private readonly PostgreSqlCommandStringCache _commandCache;
        private readonly SqlHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="headers">The headers.</param>
        public DeleteTransactionalMessageCommandHandler(IPostgreSqlMessageQueueTransportOptionsFactory options,
            PostgreSqlCommandStringCache commandCache, SqlHeaders headers)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => headers, headers);

            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(options.Create);
            _commandCache = commandCache;
            _headers = headers;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public long Handle(DeleteTransactionalMessageCommand command)
        {
            var connection = command.MessageContext.Get(_headers.Connection);
            using (var commandSql = connection.CreateCommand())
            {
                //set ID
                commandSql.Parameters.Add("@QueueID", NpgsqlDbType.Bigint);
                commandSql.Parameters["@QueueID"].Value = command.QueueId;

                //delete the meta data record
                commandSql.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.DeleteFromMetaData);
                commandSql.ExecuteNonQuery();

                //delete the message body
                commandSql.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.DeleteFromQueue);
                commandSql.ExecuteNonQuery();

                //delete any error tracking information
                commandSql.CommandText =
                    _commandCache.GetCommand(PostgreSqlCommandStringTypes.DeleteFromErrorTracking);
                commandSql.ExecuteNonQuery();

                //delete status record
                if (!_options.Value.EnableStatusTable) return 1;

                commandSql.CommandText = _commandCache.GetCommand(PostgreSqlCommandStringTypes.DeleteFromStatus);
                commandSql.ExecuteNonQuery();
                return 1;
            }
        }
    }
}
