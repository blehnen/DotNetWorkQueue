﻿// ---------------------------------------------------------------------
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
using System.Threading;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    public class RedisQueueClearErrorMessages: IClearErrorMessages
    {
        private readonly IQueryHandler<GetErrorRecordsToDeleteQuery, List<string>> _getErrorMessages;
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, bool> _deleteMessage;
        public RedisQueueClearErrorMessages(IQueryHandler<GetErrorRecordsToDeleteQuery, List<string>> getErrorMessages,
            ICommandHandlerWithOutput<DeleteMessageCommand, bool> deleteMessage)
        {
            _getErrorMessages = getErrorMessages;
            _deleteMessage = deleteMessage;
        }
        public long ClearMessages(CancellationToken cancelToken)
        {
            var count = 0L;
            if (cancelToken.IsCancellationRequested)
                return count;

            var messages = _getErrorMessages.Handle(new GetErrorRecordsToDeleteQuery());
            while (messages.Count > 0)
            {
                foreach (var message in messages)
                {
                    if (cancelToken.IsCancellationRequested)
                        return count;

                    if (_deleteMessage.Handle(new DeleteMessageCommand(new RedisQueueId(message))))
                    {
                        count++;
                    }
                }

                messages = _getErrorMessages.Handle(new GetErrorRecordsToDeleteQuery());
            }

            return count;
        }
    }
}