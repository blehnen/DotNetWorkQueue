// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Collections.Generic;
using System.Globalization;

namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class MessageInterceptorDecorator: IMessageInterceptor
    {
        private readonly IMessageInterceptor _handler;

        private readonly ITimer _metricTimerBytes;
        private readonly ITimer _metricTimerMessage;
        private readonly IHistogram _metricHistogram;
        private readonly IHistogram _metricHistogramDelta;
        private readonly IHistogram _metricHistogramOptOut;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageInterceptorDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public MessageInterceptorDecorator(IMetrics metrics,
            IMessageInterceptor handler,
            IConnectionInformation connectionInformation)
        {
            _handler = handler;
            DisplayName = _handler.DisplayName;
            var name = "MessageInterceptor";

            _metricTimerBytes =
                metrics.Timer($"{connectionInformation.QueueName}.{name}.BytesToMessageTimer", Units.Calls);

            _metricTimerMessage =
                metrics.Timer($"{connectionInformation.QueueName}.{name}.MessageToBytesTimer", Units.Calls);

            _metricHistogram = metrics.Histogram($"{connectionInformation.QueueName}.{name}.MessageToBytesHistogram", Units.Bytes,
                SamplingTypes.LongTerm);

            _metricHistogramDelta = metrics.Histogram($"{connectionInformation.QueueName}.{name}.MessageToBytesDeltaHistogram", Units.Bytes,
                SamplingTypes.LongTerm);

            _metricHistogramOptOut = metrics.Histogram($"{connectionInformation.QueueName}.{name}.OptOutOfGraphHistogram",
                Units.Bytes, SamplingTypes.LongTerm);
        }

        /// <summary>
        /// Runs the interceptor on the input and returns the output as a byte array. Used to serialize a message stream.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="headers">the message headers</param>
        /// <returns></returns>
        public MessageInterceptorResult MessageToBytes(byte[] input, IReadOnlyDictionary<string, object> headers)
        {
            using (_metricTimerMessage.NewContext())
            {
                var temp = _handler.MessageToBytes(input, headers);
                if (!temp.AddToGraph)
                {
                    _metricHistogramOptOut.Update(input.Length, input.Length.ToString(CultureInfo.InvariantCulture));
                    return temp;
                }

                //metrics.net no longer accepts negative values - which makes sense...  Ensure that the delta is always positive
                var delta = Math.Abs(input.Length - temp.Output.Length);
                _metricHistogramDelta.Update(delta, delta.ToString(CultureInfo.InvariantCulture));
                _metricHistogram.Update(input.Length, input.Length.ToString(CultureInfo.InvariantCulture));
                return temp;
            }
        }

        /// <summary>
        /// Runs the interceptor on the input and returns the output as a byte array. Used to re-construct a message stream.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="headers">the message headers</param>
        /// <returns></returns>
        public byte[] BytesToMessage(byte[] input, IReadOnlyDictionary<string, object> headers)
        {
            using (_metricTimerBytes.NewContext())
            {
                return _handler.BytesToMessage(input, headers);
            }
        }

        public string DisplayName { get; }

        /// <summary>
        /// The base type of the interceptor; used for re-creation
        /// </summary>
        /// <value>
        /// The type of the base.
        /// </value>
        public Type BaseType => _handler.BaseType;
    }
}
