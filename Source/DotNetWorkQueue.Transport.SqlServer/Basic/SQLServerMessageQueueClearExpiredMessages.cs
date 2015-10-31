// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Transport.SqlServer.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Basic.Query;
namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Deletes expired messages from the queue
    /// </summary>
    internal class SqlServerMessageQueueClearExpiredMessages: IClearExpiredMessages
    {
        #region Member Level Variables
        private readonly IConnectionInformation _connectionInfo;
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, long> _deleteMessageCommandHandler;
        private readonly IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>
            _findExpiredMessagesQueryHandler;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerMessageQueueClearExpiredMessages" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="findExpiredMessagesQueryHandler">The find expired messages query handler.</param>
        /// <param name="deleteMessageCommandHandler">The delete message command handler.</param>
        public SqlServerMessageQueueClearExpiredMessages(IConnectionInformation connectionInfo,
            IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>> findExpiredMessagesQueryHandler, 
            ICommandHandlerWithOutput<DeleteMessageCommand, long> deleteMessageCommandHandler)
        {
            Guard.NotNull(() => connectionInfo, connectionInfo);
            Guard.NotNull(() => findExpiredMessagesQueryHandler, findExpiredMessagesQueryHandler);
            Guard.NotNull(() => deleteMessageCommandHandler, deleteMessageCommandHandler);

            _connectionInfo = connectionInfo;
            _findExpiredMessagesQueryHandler = findExpiredMessagesQueryHandler;
            _deleteMessageCommandHandler = deleteMessageCommandHandler;
        }
        #endregion

        #region IClearExpiredMessages

        /// <summary>
        /// Clears the expired messages from the queue
        /// </summary>
        /// <param name="cancelToken">The cancel token. If fired, stop processing</param>
        public long ClearMessages(CancellationToken cancelToken)
        {
            return string.IsNullOrEmpty(_connectionInfo?.ConnectionString) 
                ? 
                    0 
                : 
                    _findExpiredMessagesQueryHandler.Handle(new FindExpiredMessagesToDeleteQuery(cancelToken)).Aggregate<long, long>
                        (0, (current, queueId) => current + _deleteMessageCommandHandler.Handle(new DeleteMessageCommand(queueId)));
        }

        #endregion
    }
}
