// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Threading;

namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class ClearErrorMessagesDecorator : IClearErrorMessages
    {
        private readonly IClearErrorMessages _handler;
        private readonly ITimer _timer;
        private readonly ICounter _counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="IClearExpiredMessages" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public ClearErrorMessagesDecorator(IMetrics metrics,
            IClearErrorMessages handler,
            IConnectionInformation connectionInformation)
        {
            var name = "ClearErrorMessages";
            _timer = metrics.Timer($"{connectionInformation.QueueName}.{name}.ResetTimer", Units.Calls);
            _counter = metrics.Counter($"{connectionInformation.QueueName}.{name}.ResetCounter", Units.Items);
            _handler = handler;
        }

        /// <inheritdoc />
        public long ClearMessages(CancellationToken cancelToken)
        {
            using (_timer.NewContext())
            {
                var count = _handler.ClearMessages(cancelToken);
                _counter.Increment(count);
                return count;
            }
        }
    }
}
