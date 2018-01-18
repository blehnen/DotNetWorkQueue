using System;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class RollbackMessageCommandHandler : ICommandHandler<RollbackMessageCommand>
    {
        private readonly RollbackLua _rollbackLua;
        private readonly RollbackDelayLua _rollbackDelayLua;
        private readonly bool _rpcQueue;
        private readonly IUnixTimeFactory _unixTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="rollbackLua">The rollback.</param>
        /// <param name="rollbackDelayLua">The rollback delay.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="queueContext">The queue context.</param>
        public RollbackMessageCommandHandler(RollbackLua rollbackLua, 
            RollbackDelayLua rollbackDelayLua, 
            IUnixTimeFactory unixTimeFactory,
            QueueContext queueContext)
        {
            Guard.NotNull(() => rollbackLua, rollbackLua);
            Guard.NotNull(() => rollbackDelayLua, rollbackDelayLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => queueContext, queueContext);

            _rollbackLua = rollbackLua;
            _rollbackDelayLua = rollbackDelayLua;
            _unixTimeFactory = unixTimeFactory;
            _rpcQueue = queueContext.Context == QueueContexts.RpcQueue;
        }

        /// <inheritdoc />
        public void Handle(RollbackMessageCommand command)
        {
            if (command.IncreaseQueueDelay.HasValue && command.IncreaseQueueDelay.Value != TimeSpan.Zero)
            {
                var unixTimestamp = _unixTimeFactory.Create().GetAddDifferenceMilliseconds(command.IncreaseQueueDelay.Value);
                _rollbackDelayLua.Execute(command.Id.Id.Value.ToString(), unixTimestamp);
            }
            else
            {
                _rollbackLua.Execute(command.Id.Id.Value.ToString(), _rpcQueue);
            }
        }
    }
}
