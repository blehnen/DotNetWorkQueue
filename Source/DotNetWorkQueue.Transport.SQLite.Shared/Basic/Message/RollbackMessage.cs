using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.Message
{
    /// <summary>
    /// Rolls back a message by either rolling back a transaction or updating a status
    /// </summary>
    internal class RollbackMessage
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICommandHandler<RollbackMessageCommand> _rollbackCommand;
        private readonly IIncreaseQueueDelay _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessage"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="rollbackCommand">The rollback command.</param>
        /// <param name="headers">The headers.</param>
        public RollbackMessage(QueueConsumerConfiguration configuration,
            ICommandHandler<RollbackMessageCommand> rollbackCommand,
            IIncreaseQueueDelay headers)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => rollbackCommand, rollbackCommand);
            Guard.NotNull(() => headers, headers);

            _configuration = configuration;
            _rollbackCommand = rollbackCommand;
            _headers = headers;
        }
        /// <summary>
        /// Rollbacks the specified message by setting the status
        /// </summary>
        /// <param name="context">The context.</param>
        public void Rollback(IMessageContext context)
        {
            if (context.MessageId == null || !context.MessageId.HasValue) return;

            //there is nothing to rollback unless at least one of these options is enabled
            if (_configuration.Options().EnableDelayedProcessing ||
                _configuration.Options().EnableHeartBeat ||
                _configuration.Options().EnableStatus)
            {
                DateTime? lastHeartBeat = null;
                if (context.WorkerNotification?.HeartBeat?.Status?.LastHeartBeatTime != null)
                {
                    lastHeartBeat = context.WorkerNotification.HeartBeat.Status.LastHeartBeatTime.Value;
                }

                var increaseDelay = context.Get(_headers.QueueDelay).IncreaseDelay;
                _rollbackCommand.Handle(new RollbackMessageCommand(lastHeartBeat,
                    (long)context.MessageId.Id.Value, increaseDelay));
            }
        }
    }
}
