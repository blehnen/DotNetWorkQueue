// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue
{
    /// <inheritdoc cref="IDisposable" />
    /// <inheritdoc cref="IIsDisposed" />
    /// <summary>
    /// Sends a job to a queue
    /// </summary>
    public interface ISendJobToQueue: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Sends the specified action to the specified queue.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="expressionToRun">The expression to run.</param>
        /// <returns></returns>
        Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, LinqExpressionToRun expressionToRun);
        /// <summary>
        /// Sends the specified action to the specified queue.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="actionToRun">The action to run.</param>
        /// <param name="rawExpression">if set to <c>true</c> this expression will not be serialized. This will fail unless an in-process queue is being used.</param>
        /// <returns></returns>
        Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> actionToRun, bool rawExpression = false);

        /// <summary>
        /// The configuration settings for the queue.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        QueueProducerConfiguration Configuration { get; }
    }
}
