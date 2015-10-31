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

using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Redis.Basic.MessageID;
namespace DotNetWorkQueue.Transport.Redis.Basic.Factory
{
    /// <summary>
    /// Creates new instances of <see cref="IGetMessageId"/>
    /// </summary>
    internal class GetMessageIdFactory : IGetMessageIdFactory
    {
        private readonly IContainerFactory _container;
        private readonly RedisQueueTransportOptions _options;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetMessageIdFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="options">The options.</param>
        public GetMessageIdFactory(IContainerFactory container, RedisQueueTransportOptions options)
        {
            Guard.NotNull(() => container, container);
            Guard.NotNull(() => options, options);

            _container = container;
            _options = options;
        }
        /// <summary>
        /// Creates new instance of <see cref="IGetMessageId"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException"></exception>
        public IGetMessageId Create()
        {
            switch (_options.MessageIdLocation)
            {
                case MessageIdLocations.Uuid:
                    return _container.Create().GetInstance<GetUuidMessageId>();
                case MessageIdLocations.RedisIncr:
                    return _container.Create().GetInstance<GetRedisIncrId>();
                case MessageIdLocations.Custom:
                    return _container.Create().GetInstance<IGetMessageId>();
                default:
                    throw new DotNetWorkQueueException($"unhandled type of {_options.MessageIdLocation}");
            }
        }
    }
}
