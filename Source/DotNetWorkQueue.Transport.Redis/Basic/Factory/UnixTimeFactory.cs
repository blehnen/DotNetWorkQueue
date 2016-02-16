// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Transport.Redis.Basic.Time;
namespace DotNetWorkQueue.Transport.Redis.Basic.Factory
{
    /// <summary>
    /// Creates new instances of <see cref="IUnixTime"/>
    /// </summary>
    internal class UnixTimeFactory : IUnixTimeFactory
    {
        private readonly IContainerFactory _container;
        private readonly RedisQueueTransportOptions _options;
        /// <summary>
        /// Initializes a new instance of the <see cref="UnixTimeFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="options">The options.</param>
        public UnixTimeFactory(IContainerFactory container, RedisQueueTransportOptions options)
        {
            Guard.NotNull(() => container, container);
            Guard.NotNull(() => options, options);

            _container = container;
            _options = options;
        }

        /// <summary>
        /// Returns an instance of <see cref="IUnixTime" />
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException"></exception>
        public IUnixTime Create()
        {
            switch (_options.TimeServer)
            {
                case TimeLocations.LocalMachine:
                    return _container.Create().GetInstance<LocalMachineUnixTime>();
                case TimeLocations.RedisServer:
                    return _container.Create().GetInstance<RedisServerUnixTime>();
                case TimeLocations.SntpServer:
                    return _container.Create().GetInstance<SntpUnixTime>();
                case TimeLocations.Custom:
                    return _container.Create().GetInstance<IUnixTime>();
                default:
                    throw new DotNetWorkQueueException($"unhandled type of {_options.TimeServer}");
            }
        }
    }
}
