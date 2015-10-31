// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
    internal class InternalSerializerDecorator: IInternalSerializer
    {
        private readonly ITimer _messageToBytesTimer;
        private readonly ITimer _bytesToMessageTimer;
        private readonly ITimer _messageToStringTimer;
        private readonly IHistogram _resultSizeHistogram;
        private readonly IHistogram _resultSizeStringHistogram;
        private readonly IInternalSerializer _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalSerializerDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public InternalSerializerDecorator(IMetrics metrics,
            IInternalSerializer handler,
            IConnectionInformation connectionInformation)
        {
            var name = handler.GetType().Name;
            _bytesToMessageTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ConvertBytesToTimer", Units.Calls);
            _messageToBytesTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ConvertToBytesTimer", Units.Calls);
            _messageToStringTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ConvertToStringTimer", Units.Calls);
            _resultSizeHistogram = metrics.Histogram($"{connectionInformation.QueueName}.{name}.ConvertToBytesHistogram", Units.Bytes,
                SamplingTypes.LongTerm);
            _resultSizeStringHistogram = metrics.Histogram($"{connectionInformation.QueueName}.{name}.ConvertToStringHistogram", Units.Bytes,
                SamplingTypes.LongTerm);
            _handler = handler;
        }

        /// <summary>
        /// Converts an input class to bytes.
        /// </summary>
        /// <typeparam name="T">Input type</typeparam>
        /// <param name="data">The data to serialize</param>
        /// <returns></returns>
        public byte[] ConvertToBytes<T>(T data) where T : class
        {
            using (_messageToBytesTimer.NewContext())
            {
                var result = _handler.ConvertToBytes(data);
                _resultSizeHistogram.Update(result.Length, result.Length.ToString(CultureInfo.InvariantCulture));
                return result;
            }
        }

        /// <summary>
        /// Converts the bytes back to the input class
        /// </summary>
        /// <typeparam name="T">output type</typeparam>
        /// <param name="bytes">The data to de-serialize.</param>
        /// <returns></returns>
        public T ConvertBytesTo<T>(byte[] bytes) where T : class
        {
            using (_bytesToMessageTimer.NewContext())
            {
                return _handler.ConvertBytesTo<T>(bytes);
            }
        }

        /// <summary>
        /// Converts an input class to a string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data to serialize</param>
        /// <returns></returns>
        public string ConvertToString<T>(T data) where T : class
        {
            using (_messageToStringTimer.NewContext())
            {
                var result = _handler.ConvertToString(data);
                _resultSizeStringHistogram.Update(result.Length, result.Length.ToString(CultureInfo.InvariantCulture));
                return result;
            }
        }
    }
}
