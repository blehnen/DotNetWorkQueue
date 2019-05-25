using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Redis.Basic.Command;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Handles receiving a poison message - a message that cannot be deserialized
    /// </summary>
    internal class RedisQueueReceivePoisonMessage : IReceivePoisonMessage
    {
        private readonly ICommandHandler<MoveRecordToErrorQueueCommand> _commandMoveRecord;
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueReceivePoisonMessage"/> class.
        /// </summary>
        /// <param name="commandMoveRecord">The command move record.</param>
        public RedisQueueReceivePoisonMessage(ICommandHandler<MoveRecordToErrorQueueCommand> commandMoveRecord)
        {
            _commandMoveRecord = commandMoveRecord;
        }

        /// <summary>
        /// Invoked when we have dequeued a message, but a failure occurred during re-assembly.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        public void Handle(IMessageContext context, PoisonMessageException exception)
        {
            if (context.MessageId != null && context.MessageId.HasValue)
            {
                _commandMoveRecord.Handle(new MoveRecordToErrorQueueCommand((RedisQueueId)context.MessageId));
            }
            context.SetMessageAndHeaders(null, context.Headers);
        }
    }
}
