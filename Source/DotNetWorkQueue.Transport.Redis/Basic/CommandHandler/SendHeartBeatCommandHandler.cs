using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class SendHeartBeatCommandHandler: ICommandHandlerWithOutput<SendHeartBeatCommand, long>
    {
        private readonly SendHeartbeatLua _sendHeartbeatLua;
        private readonly IUnixTimeFactory _unixTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="sendHeartbeatLua">The send heartbeat lua.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        public SendHeartBeatCommandHandler(SendHeartbeatLua sendHeartbeatLua, 
            IUnixTimeFactory unixTimeFactory)
        {
            Guard.NotNull(() => sendHeartbeatLua, sendHeartbeatLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);

            _sendHeartbeatLua = sendHeartbeatLua;
            _unixTimeFactory = unixTimeFactory;
        }

        /// <inheritdoc />
        public long Handle(SendHeartBeatCommand command)
        {
            var date = _unixTimeFactory.Create().GetCurrentUnixTimestampMilliseconds();
            _sendHeartbeatLua.Execute(command.QueueId.Id.Value.ToString(), date);
            return date;
        }
    }
}
