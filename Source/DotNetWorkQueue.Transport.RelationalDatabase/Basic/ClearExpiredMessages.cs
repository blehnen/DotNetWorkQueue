// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <inheritdoc />
    public class ClearExpiredMessages: IClearExpiredMessages
    {
        #region Member Level Variables
        private readonly IConnectionInformation _connectionInfo;
        private readonly IRemoveMessage _removeMessage;
        private readonly IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>
            _findExpiredMessagesQueryHandler;
        #endregion

        #region Constructor
        /// <summary>Initializes a new instance of the <see cref="ClearExpiredMessages"/> class.</summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="findExpiredMessagesQueryHandler">The find expired messages query handler.</param>
        /// <param name="removeMessage"></param>
        public ClearExpiredMessages(IConnectionInformation connectionInfo,
            IQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>> findExpiredMessagesQueryHandler,
            IRemoveMessage removeMessage)
        {
            Guard.NotNull(() => connectionInfo, connectionInfo);
            Guard.NotNull(() => findExpiredMessagesQueryHandler, findExpiredMessagesQueryHandler);
            Guard.NotNull(() => removeMessage, removeMessage);

            _connectionInfo = connectionInfo;
            _findExpiredMessagesQueryHandler = findExpiredMessagesQueryHandler;
            _removeMessage = removeMessage;
        }
        #endregion

        #region IClearExpiredMessages

        /// <inheritdoc />
        public long ClearMessages(CancellationToken cancelToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionInfo?.ConnectionString))
                return 0;

            var messages = _findExpiredMessagesQueryHandler.Handle(new FindExpiredMessagesToDeleteQuery(cancelToken));
            var count = 0;
            foreach (var message in messages)
            {
                var status = _removeMessage.Remove(new MessageQueueId(message), RemoveMessageReason.Expired);
                if (status == RemoveMessageStatus.Removed)
                    count++;
            }
            return count;
        }

        #endregion
    }
}
