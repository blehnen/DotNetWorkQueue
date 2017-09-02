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

using System.Globalization;

namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class SerializerDecorator: ISerializer
    {
        private readonly ITimer _messageToBytesTimer;
        private readonly ITimer _bytesToMessageTimer;
        private readonly IHistogram _resultSizeHistogram;
        private readonly ISerializer _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SerializerDecorator(IMetrics metrics,
            ISerializer handler,
            IConnectionInformation connectionInformation)
        {
            var name = handler.GetType().Name;
            _bytesToMessageTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ConvertBytesToMessageTimer", Units.Calls);
            _messageToBytesTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ConvertMessageToBytesTimer", Units.Calls);
            _resultSizeHistogram = metrics.Histogram($"{connectionInformation.QueueName}.{name}.ConvertMessageToBytesHistogram", Units.Bytes,
                SamplingTypes.LongTerm);
            _handler = handler;
        }

        /// <summary>
        /// Converts the message to an array of bytes
        /// </summary>
        /// <typeparam name="T">the message type</typeparam>
        /// <param name="message">The message.</param>
        /// <returns>
        /// byte array
        /// </returns>
        public byte[] ConvertMessageToBytes<T>(T message) where T : class
        {
            using (_messageToBytesTimer.NewContext())
            {
                var result = _handler.ConvertMessageToBytes(message);
                _resultSizeHistogram.Update(result.Length, result.Length.ToString(CultureInfo.InvariantCulture));
                return result;
            }
        }

        /// <summary>
        /// Converts the byte array to a message.
        /// </summary>
        /// <typeparam name="T">the message type</typeparam>
        /// <param name="bytes">The bytes.</param>
        /// <returns>
        /// an instance of T
        /// </returns>
        public T ConvertBytesToMessage<T>(byte[] bytes) where T : class
        {
            using (_bytesToMessageTimer.NewContext())
            {
                return _handler.ConvertBytesToMessage<T>(bytes);
            }
        }
    }
}
