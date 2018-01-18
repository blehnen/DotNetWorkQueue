using System;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
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
        /// <summary>
        /// Gets the first possible delivery time for a message in UTC format.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The additional message data.</param>
        /// <returns></returns>
        public DateTime GetTime(IMessage message, IAdditionalMessageData data)
        {
            var delay = data.GetDelay();
            return delay.HasValue ? _getTime.Create().GetCurrentUtcDate().Add(delay.Value) : _getTime.Create().GetCurrentUtcDate();
        }
    }
}
