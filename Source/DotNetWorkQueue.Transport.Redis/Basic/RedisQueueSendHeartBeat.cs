using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Sends a heartbeat for a worker
    /// </summary>
    internal class RedisQueueSendHeartBeat: ISendHeartBeat
    {
        private readonly ICommandHandlerWithOutput<SendHeartBeatCommand, long> _sendHeartBeat;
        private readonly IUnixTimeFactory _unixTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueSendHeartBeat"/> class.
        /// </summary>
        /// <param name="sendHeartBeat">The send heart beat.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        public RedisQueueSendHeartBeat(ICommandHandlerWithOutput<SendHeartBeatCommand, long> sendHeartBeat, 
            IUnixTimeFactory unixTimeFactory)
        {
            Guard.NotNull(() => sendHeartBeat, sendHeartBeat);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);

            _sendHeartBeat = sendHeartBeat;
            _unixTimeFactory = unixTimeFactory;
        }

        /// <summary>
        /// Updates the heart beat for a record.
        /// </summary>
        /// <param name="context">The context.</param>
        public IHeartBeatStatus Send(IMessageContext context)
        {
            if (context.MessageId == null || !context.MessageId.HasValue) return null;
            var unixTime = _sendHeartBeat.Handle(new SendHeartBeatCommand((RedisQueueId)context.MessageId));
            return new HeartBeatStatus(context.MessageId, _unixTimeFactory.Create().DateTimeFromUnixTimestampMilliseconds(unixTime)); //UTC 
        }
    }
}
