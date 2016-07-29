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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Sends jobs to a transport
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IQueue" />
    public interface IProducerMethodJobQueue: IQueue
    {
        /// <summary>
        /// Sends the specified linqExpression to be executed.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="method">The linqExpression to execute.</param>
        /// <returns></returns>
        Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method);

        /// <summary>
        /// Sends the specified dynamic linqExpression to be executed.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="linqExpression">The linqExpression to execute.</param>
        /// <returns></returns>
        Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, LinqExpressionToRun linqExpression);

        /// <summary>
        /// Gets the last known event.
        /// </summary>
        /// <returns></returns>
        IJobSchedulerLastKnownEvent LastKnownEvent { get; }

        /// <summary>
        /// The configuration settings for the queue.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        QueueProducerConfiguration Configuration { get; }

        /// <summary>
        /// Gets the queue specific logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        ILog Logger { get; }
    }
}
