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
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// Creates new instances of <see cref="IQueueWait"/>
    /// </summary>
    internal class QueueWaitFactory : IQueueWaitFactory
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICancelWork _cancelWork;
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueWaitFactory" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="cancelWork">The cancel work.</param>
        public QueueWaitFactory(QueueConsumerConfiguration configuration, IQueueCancelWork cancelWork)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => cancelWork, cancelWork);

            _configuration = configuration;
            _cancelWork = cancelWork;
        }
        /// <summary>
        /// Creates a new <see cref="IQueueWait" /> instance
        /// </summary>
        /// <returns></returns>
        public IQueueWait CreateQueueDelay()
        {
            if (_configuration.TransportConfiguration.QueueDelayBehavior != null && _configuration.TransportConfiguration.QueueDelayBehavior.Any())
            {
                return new QueueWait(_configuration.TransportConfiguration.QueueDelayBehavior, _cancelWork);
            }
            return new QueueWaitNoOp();
        }

        /// <summary>
        /// Creates a <see cref="IQueueWait" /> for fatal errors
        /// </summary>
        /// <returns></returns>
        public IQueueWait CreateFatalErrorDelay()
        {
            if (_configuration.TransportConfiguration.FatalExceptionDelayBehavior != null && _configuration.TransportConfiguration.FatalExceptionDelayBehavior.Any())
            {
                return new QueueWait(_configuration.TransportConfiguration.FatalExceptionDelayBehavior, _cancelWork);
            }
            return new QueueWaitNoOp();
        }
    }
}
