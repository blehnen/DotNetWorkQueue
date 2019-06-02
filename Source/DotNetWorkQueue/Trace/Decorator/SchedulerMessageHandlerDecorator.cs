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
