// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IProducerMethodJobQueue" />
    public class ProducerMethodJobQueueDecorator : IProducerMethodJobQueue
    {
        private readonly ActivitySource _tracer;
        private readonly IProducerMethodJobQueue _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProducerMethodJobQueueDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        public ProducerMethodJobQueueDecorator(IProducerMethodJobQueue handler, ActivitySource tracer)
        {
            _handler = handler;
            _tracer = tracer;
        }

        /// <inheritdoc />
        public IJobSchedulerLastKnownEvent LastKnownEvent => _handler.LastKnownEvent;

        /// <inheritdoc />
        public QueueProducerConfiguration Configuration => _handler.Configuration;

        /// <inheritdoc />
        public ILogger Logger => _handler.Logger;

        /// <inheritdoc />
        public bool IsDisposed => _handler.IsDisposed;

        /// <inheritdoc />
        public void Dispose()
        {
            _handler.Dispose();
        }

        /// <inheritdoc />
        public async Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method, bool rawExpression = false)
        {
            using (var scope = _tracer.StartActivity("SendJobAsync"))
            {
                scope?.SetTag("JobName", job.Name);
                return await _handler.SendAsync(job, scheduledTime, method, rawExpression);
            }
        }

#if NETFULL
        /// <summary>
        /// Sends the specified dynamic linqExpression to be executed.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <param name="linqExpression">The linqExpression to execute.</param>
        /// <returns></returns>
        public async Task<IJobQueueOutputMessage> SendAsync(IScheduledJob job, DateTimeOffset scheduledTime, LinqExpressionToRun linqExpression)
        {
            using (var scope = _tracer.StartActivity("SendJobAsync"))
            {
                scope?.SetTag("JobName", job.Name);
                return await _handler.SendAsync(job, scheduledTime, linqExpression);
            }
        }
#endif

        /// <inheritdoc />
        public void Start()
        {
            _handler.Start();
        }
    }
}
