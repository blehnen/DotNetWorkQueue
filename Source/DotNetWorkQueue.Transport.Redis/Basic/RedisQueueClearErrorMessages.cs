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
