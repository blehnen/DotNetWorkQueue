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
using System;
using System.Threading.Tasks;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.ISchedulerMessageHandler" />
    public class SchedulerMessageHandlerDecorator: ISchedulerMessageHandler
    {
        private readonly ITracer _tracer;
        private readonly ISchedulerMessageHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessageDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        public SchedulerMessageHandlerDecorator(ISchedulerMessageHandler handler, ITracer tracer)
        {
            _handler = handler;
            _tracer = tracer;
        }

        /// <inheritdoc />
        public Task HandleAsync<T>(IWorkGroup workGroup, IReceivedMessage<T> message, IWorkerNotification notifications, Action<IReceivedMessage<T>, IWorkerNotification> functionToRun, ITaskFactory taskFactory) where T : class
        {
            using (IScope scope = _tracer.BuildSpan("SchedulerMessageHandler").StartActive(finishSpanOnDispose: true))
            {
                return _handler.HandleAsync(workGroup, message, notifications, functionToRun, taskFactory);
            }
        }
    }
}
