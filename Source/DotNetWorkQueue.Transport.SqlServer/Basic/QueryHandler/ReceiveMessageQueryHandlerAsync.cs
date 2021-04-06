// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Data.SqlClient;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    /// <summary>
    /// Dequeues a message.
    /// </summary>
    internal class ReceiveMessageQueryHandlerAsync : IQueryHandler<ReceiveMessageQueryAsync<SqlConnection, SqlTransaction>, Task<IReceivedMessageInternal>>
    {
        private readonly BuildDequeueCommand _buildDequeueCommand;
        private readonly ReadMessage _readMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.ReceiveMessageQueryHandler" /> class.
        /// </summary>
        /// <param name="readMessage">The read message.</param>
        /// <param name="buildDequeueCommand">The build dequeue command.</param>
        public ReceiveMessageQueryHandlerAsync(ReadMessage readMessage,
            BuildDequeueCommand buildDequeueCommand)
        {
            Guard.NotNull(() => readMessage, readMessage);
            Guard.NotNull(() => buildDequeueCommand, buildDequeueCommand);

            _readMessage = readMessage;
            _buildDequeueCommand = buildDequeueCommand;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public async Task<IReceivedMessageInternal> Handle(ReceiveMessageQueryAsync<SqlConnection, SqlTransaction> query)
        {
            using (var selectCommand = query.Connection.CreateCommand())
            {
                _buildDequeueCommand.BuildCommand(selectCommand, query);
                using (var reader = await selectCommand.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    return _readMessage.Read(reader);
                }
            }
        }
    }
}
