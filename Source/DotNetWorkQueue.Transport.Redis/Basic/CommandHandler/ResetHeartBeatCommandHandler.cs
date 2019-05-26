using System.Collections.Generic;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class ResetHeartBeatCommandHandler : ICommandHandlerWithOutput<ResetHeartBeatCommand, List<ResetHeartBeatOutput>>
    {
        private readonly IHeartBeatConfiguration _configuration;
        private readonly ResetHeartbeatLua _resetHeartbeatLua;
        private readonly bool _rpcQueue;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly RedisQueueTransportOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="resetHeartbeatLua">The reset heartbeat lua.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="options">The options.</param>
        /// <param name="queueContext">The queue context.</param>
        public ResetHeartBeatCommandHandler(IHeartBeatConfiguration configuration,
            ResetHeartbeatLua resetHeartbeatLua, 
            IUnixTimeFactory unixTimeFactory, 
            RedisQueueTransportOptions options,
            QueueContext queueContext)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => resetHeartbeatLua, resetHeartbeatLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => queueContext, queueContext);

            _configuration = configuration;
            _resetHeartbeatLua = resetHeartbeatLua;
            _unixTimeFactory = unixTimeFactory;
            _options = options;
            _rpcQueue = queueContext.Context == QueueContexts.RpcQueue;
        }

        /// <inheritdoc />
        public List<ResetHeartBeatOutput> Handle(ResetHeartBeatCommand command)
        {
            return _resetHeartbeatLua.Execute(_unixTimeFactory.Create().GetSubtractDifferenceMilliseconds(_configuration.Time), _options.ResetHeartBeatBatchLimit, _rpcQueue);
        }
    }
}
