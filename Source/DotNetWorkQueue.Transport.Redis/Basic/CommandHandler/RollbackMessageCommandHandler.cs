using System;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class RollbackMessageCommandHandler : ICommandHandler<RollbackMessageCommand>
    {
        private readonly RollbackLua _rollbackLua;
        private readonly RollbackDelayLua _rollbackDelayLua;
        private readonly IUnixTimeFactory _unixTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="rollbackLua">The rollback.</param>
        /// <param name="rollbackDelayLua">The rollback delay.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        public RollbackMessageCommandHandler(RollbackLua rollbackLua, 
            RollbackDelayLua rollbackDelayLua, 
            IUnixTimeFactory unixTimeFactory)
        {
            Guard.NotNull(() => rollbackLua, rollbackLua);
            Guard.NotNull(() => rollbackDelayLua, rollbackDelayLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            _rollbackLua = rollbackLua;
            _rollbackDelayLua = rollbackDelayLua;
            _unixTimeFactory = unixTimeFactory;
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
                _rollbackLua.Execute(command.Id.Id.Value.ToString());
            }
        }
    }
}
