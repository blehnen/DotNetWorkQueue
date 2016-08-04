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

using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// Creates new instance of <see cref="IHeartBeatThreadPool"/>
    /// </summary>
    public class HeartBeatThreadPoolFactory : IHeartBeatThreadPoolFactory
    {
        private readonly IContainerFactory _container;
        /// <summary>
        /// sync lock for starting the thread pool
        /// </summary>
        /// <remarks>Multiple requests may be made for the singleton pool at the same time. We need to ensure we only start it once.</remarks>
        private readonly object _poolStart = new object();
        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatThreadPoolFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public HeartBeatThreadPoolFactory(IContainerFactory container)
        {
            Guard.NotNull(() => container, container);
            _container = container;
        }
        /// <summary>
        /// Creates new instance of <see cref="IHeartBeatThreadPool" />
        /// </summary>
        /// <returns></returns>
        public IHeartBeatThreadPool Create()
        {
            var pool = _container.Create().GetInstance<IHeartBeatThreadPool>();
            if (pool.IsStarted) return pool;
            lock (_poolStart)
            {
                if (!pool.IsStarted) //now that we have a lock, re-verify
                {
                    pool.Start();
                }
            }
            return pool;
        }
    }
}
