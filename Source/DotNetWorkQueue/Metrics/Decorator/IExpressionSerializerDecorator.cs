// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System.Globalization;
using System.Linq.Expressions;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Metrics.Decorator
{

    /// <summary>
    /// Captures metrics for expression serialization
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IExpressionSerializer" />
    internal class ExpressionSerializerDecorator: IExpressionSerializer
    {
        private readonly ITimer _methodToBytesTimer;
        private readonly ITimer _bytesToMethodTimer;

        private readonly ITimer _functionToBytesTimer;
        private readonly ITimer _bytesToFunctionTimer;

        private readonly IHistogram _resultMethodSizeHistogram;
        private readonly IHistogram _resultFunctionSizeHistogram;

        private readonly IExpressionSerializer _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionSerializerDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public ExpressionSerializerDecorator(IMetrics metrics,
            IExpressionSerializer handler,
            IConnectionInformation connectionInformation)
        {
            var name = handler.GetType().Name;
            _methodToBytesTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ConvertMethodToBytesTimer", Units.Calls);
            _bytesToMethodTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ConvertBytesToMethodTimer", Units.Calls);
            _resultMethodSizeHistogram = metrics.Histogram($"{connectionInformation.QueueName}.{name}.ConvertMethodToBytesHistogram", Units.Bytes,
                SamplingTypes.LongTerm);

            _functionToBytesTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ConvertFunctionToBytesTimer", Units.Calls);
            _bytesToFunctionTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ConvertBytesToFunctionTimer", Units.Calls);
            _resultFunctionSizeHistogram = metrics.Histogram($"{connectionInformation.QueueName}.{name}.ConvertFunctionToBytesHistogram", Units.Bytes,
                SamplingTypes.LongTerm);

            _handler = handler;
        }

        /// <summary>
        /// Converts the action method to bytes.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        public byte[] ConvertMethodToBytes(Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method)
        {
            using (_methodToBytesTimer.NewContext())
            {
                var result = _handler.ConvertMethodToBytes(method);
                _resultMethodSizeHistogram.Update(result.Length, result.Length.ToString(CultureInfo.InvariantCulture));
                return result;
            }
        }

        /// <summary>
        /// Converts the bytes to an action method.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        public Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> ConvertBytesToMethod(byte[] bytes)
        {
            using (_bytesToMethodTimer.NewContext())
            {
                return _handler.ConvertBytesToMethod(bytes);
            }
        }

        /// <summary>
        /// Converts the function to bytes.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        public byte[] ConvertFunctionToBytes(Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>> method)
        {
            using (_functionToBytesTimer.NewContext())
            {
                var result = _handler.ConvertFunctionToBytes(method);
                _resultFunctionSizeHistogram.Update(result.Length, result.Length.ToString(CultureInfo.InvariantCulture));
                return result;
            }
        }

        /// <summary>
        /// Converts the bytes back to the function.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        public Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>> ConvertBytesToFunction(byte[] bytes)
        {
            using (_bytesToFunctionTimer.NewContext())
            {
                return _handler.ConvertBytesToFunction(bytes);
            }
        }
    }
}
