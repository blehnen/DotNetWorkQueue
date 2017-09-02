// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class ReceiveMessagesDecorator: IReceiveMessages
    {
        private readonly IReceiveMessages _handler;
        private readonly IHeaders _headers;
        private readonly IMeter _meter;
        private readonly ITimer _waitTimer;
        private readonly IGetTimeFactory _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessagesDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="getTime">The get time.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public ReceiveMessagesDecorator(IMetrics metrics,
            IHeaders headers,
            IGetTimeFactory getTime,
            IReceiveMessages handler,
            IConnectionInformation connectionInformation)
        {
            var name = handler.GetType().Name;
            _meter = metrics.Meter($"{connectionInformation.QueueName}.{name}.ReceiveMessageMeter", Units.Items);
            _waitTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.WaitForDeQueueMeter", Units.None);
            _handler = handler;
            _getTime = getTime;
            _headers = headers;
        }

        /// <summary>
        /// Returns a message to process.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <returns>
        /// A message to process or null if there are no messages to process
        /// </returns>
        public IReceivedMessageInternal ReceiveMessage(IMessageContext context)
        {
            var result = _handler.ReceiveMessage(context);
            if (result != null)
            {
                ProcessResult(result);
            }
            return result;
        }

        /// <summary>
        /// Returns a message to process.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <returns>
        /// A message to process or null if there are no messages to process
        /// </returns>
        public async Task<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context)
        {
            var result = await _handler.ReceiveMessageAsync(context).ConfigureAwait(false);
            if (result != null)
            {
                ProcessResult(result);
            }
            return result;
        }

        private void ProcessResult(IReceivedMessageInternal message)
        {
            if (message.Headers.ContainsKey(_headers.StandardHeaders.FirstPossibleDeliveryDate.Name))
            {
                var waitTime =
                    (ValueTypeWrapper<DateTime>)message.Headers[_headers.StandardHeaders.FirstPossibleDeliveryDate.Name];
                if (waitTime != null)
                {
                    var difference = _getTime.Create().GetCurrentUtcDate() - waitTime.Value;
                    _waitTimer.Record((long)difference.TotalMilliseconds, TimeUnits.Milliseconds);
                }
            }
            _meter.Mark();
        }
    }
}
