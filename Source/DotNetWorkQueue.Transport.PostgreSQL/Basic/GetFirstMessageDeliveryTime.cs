using System;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
    internal class GetFirstMessageDeliveryTime: IGetFirstMessageDeliveryTime
    {
        private readonly IGetTimeFactory _getTime;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetFirstMessageDeliveryTime"/> class.
        /// </summary>
        /// <param name="getTime">The get time.</param>
        public GetFirstMessageDeliveryTime(IGetTimeFactory getTime)
        {
            _getTime = getTime;
        }
        /// <inheritdoc />
        public DateTime GetTime(IMessage message, IAdditionalMessageData data)
        {
            var delay = data.GetDelay();
            return delay.HasValue ? _getTime.Create().GetCurrentUtcDate().Add(delay.Value) : _getTime.Create().GetCurrentUtcDate();
        }
    }
}
