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
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Metrics.Decorator
{
    /// <inheritdoc />
    internal class ReceiveMessageQueryDecorator: IQueryHandler<ReceiveMessageQuery, RedisMessage>
    {
        private readonly IQueryHandler<ReceiveMessageQuery, RedisMessage> _handler;
        private readonly ICounter _counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public ReceiveMessageQueryDecorator(IMetrics metrics,
            IQueryHandler<ReceiveMessageQuery, RedisMessage> handler,
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => metrics, metrics);
            Guard.NotNull(() => handler, handler);

            var name = handler.GetType().Name;
            _counter = metrics.Counter($"{connectionInformation.QueueName}.{name}.HandleAsync.Expired", Units.Items);
            _handler = handler;
        }

        /// <inheritdoc />
        public RedisMessage Handle(ReceiveMessageQuery query)
        {
            var result = _handler.Handle(query);
            if (result != null && result.Expired)
            {
                _counter.Increment(1);
            }
            return result;
        }
    }
}
