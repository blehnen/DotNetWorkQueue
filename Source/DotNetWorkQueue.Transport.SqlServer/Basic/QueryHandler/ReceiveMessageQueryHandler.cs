// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using Microsoft.Data.SqlClient;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    /// <summary>
    /// Dequeues a message.
    /// </summary>
    internal class ReceiveMessageQueryHandler : IQueryHandler<ReceiveMessageQuery<SqlConnection, SqlTransaction>, IReceivedMessageInternal>
    {
        private readonly BuildDequeueCommand _buildDequeueCommand;
        private readonly ReadMessage _readMessage;
        private readonly IMessageClaim _messageClaim;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="readMessage">The read message.</param>
        /// <param name="buildDequeueCommand">The build dequeue command.</param>
        /// <param name="messageClaim">Records the heartbeat this de-queue stamps, so the worker can prove its claim.</param>
        public ReceiveMessageQueryHandler(ReadMessage readMessage,
            BuildDequeueCommand buildDequeueCommand,
            IMessageClaim messageClaim)
        {
            Guard.NotNull(readMessage);
            Guard.NotNull(buildDequeueCommand);

            _readMessage = readMessage;
            _messageClaim = messageClaim;
            _buildDequeueCommand = buildDequeueCommand;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IReceivedMessageInternal Handle(ReceiveMessageQuery<SqlConnection, SqlTransaction> query)
        {
            using (var selectCommand = query.Connection.CreateCommand())
            {
                _buildDequeueCommand.BuildCommand(selectCommand, query);
                using (var reader = selectCommand.ExecuteReader())
                {
                    var message = _readMessage.Read(reader, out var claimedAt);
                    if (message != null && claimedAt.HasValue && query.MessageContext != null)
                    {
                        //the stamp the database wrote, which is the value this claim is held by
                        query.MessageContext.Set(_messageClaim.ClaimedAt,
                            new ValueTypeWrapper<DateTime>(claimedAt.Value));
                    }

                    return message;
                }
            }
        }
    }
}
