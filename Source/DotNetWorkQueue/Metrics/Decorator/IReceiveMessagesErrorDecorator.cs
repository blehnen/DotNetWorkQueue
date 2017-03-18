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
namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class ReceiveMessagesErrorDecorator: IReceiveMessagesError
    {
        private readonly IReceiveMessagesError _handler;
        private readonly IMeter _meterError;
        private readonly IMeter _meterRetry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessagesErrorDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public ReceiveMessagesErrorDecorator(IMetrics metrics,
            IReceiveMessagesError handler,
            IConnectionInformation connectionInformation)
        {
            var name = handler.GetType().Name;
            _meterError = metrics.Meter($"{connectionInformation.QueueName}.{name}.MessageFailedProcessingErrorMeter", Units.Items);
            _meterRetry = metrics.Meter($"{connectionInformation.QueueName}.{name}.MessageFailedProcessingRetryMeter", Units.Items);
            _handler = handler;
        }

        /// <summary>
        /// Invoked when a message has failed to process.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>
        /// Result of error processing
        /// </returns>
        public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context,
            Exception exception)
        {
            var result = _handler.MessageFailedProcessing(message, context, exception);
            switch (result)
            {
                case ReceiveMessagesErrorResult.Error:
                    _meterError.Mark();
                    break;
                case ReceiveMessagesErrorResult.Retry:
                    _meterRetry.Mark();
                    break;
            }
            return result;
        }
    }
}
