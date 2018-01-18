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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetWorkQueue.QueueStatus
{
    /// <summary>
    /// Outputs the current status of a group of queues
    /// </summary>
    internal class QueueStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueStatus"/> class.
        /// </summary>
        /// <param name="providers">The providers.</param>
        public QueueStatus(IEnumerable<IQueueStatusProvider> providers)
        {
            var statues = new ConcurrentBag<IQueueInformation>();
            Parallel.ForEach(providers, provider =>
            {
                IQueueInformation current;
                try
                {
                    current = provider.Current;
                }
                catch (Exception error)
                {
                    current = new QueueInformationError(provider.Name, provider.Server, error);
                }
                if (current != null)
                {
                    statues.Add(current);
                }
            });
            Queues = statues.ToList();
        }
        /// <summary>
        /// Gets the queues.
        /// </summary>
        /// <value>
        /// The queues.
        /// </value>
        public IEnumerable<IQueueInformation> Queues { get; }
    }
}
