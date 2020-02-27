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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    public class ClearErrorMessages: IClearErrorMessages
    {
        #region Member Level Variables
        private readonly IConnectionInformation _connectionInfo;
        private readonly IRemoveMessage _removeMessage;
        private readonly IQueryHandler<FindErrorMessagesToDeleteQuery, IEnumerable<long>>
            _findErrorMessagesQueryHandler;
        #endregion

        #region Constructor
        /// <summary>Initializes a new instance of the <see cref="ClearExpiredMessages"/> class.</summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="findErrorMessagesQueryHandler">The find error messages query handler.</param>
        /// <param name="removeMessage"></param>
        public ClearErrorMessages(IConnectionInformation connectionInfo,
            IQueryHandler<FindErrorMessagesToDeleteQuery, IEnumerable<long>> findErrorMessagesQueryHandler,
            IRemoveMessage removeMessage)
        {
            Guard.NotNull(() => connectionInfo, connectionInfo);
            Guard.NotNull(() => findErrorMessagesQueryHandler, findErrorMessagesQueryHandler);
            Guard.NotNull(() => removeMessage, removeMessage);

            _connectionInfo = connectionInfo;
            _findErrorMessagesQueryHandler = findErrorMessagesQueryHandler;
            _removeMessage = removeMessage;
        }
        #endregion

        #region IClearErrorMessages

        /// <inheritdoc />
        public long ClearMessages(CancellationToken cancelToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionInfo?.ConnectionString))
                return 0;

            var messages = _findErrorMessagesQueryHandler.Handle(new FindErrorMessagesToDeleteQuery(cancelToken));
            var count = 0;
            foreach (var message in messages)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                var status = _removeMessage.Remove(new MessageQueueId(message), RemoveMessageReason.Error);
                if (status == RemoveMessageStatus.Removed)
                    count++;
            }
            return count;
        }

        #endregion
    }
}
