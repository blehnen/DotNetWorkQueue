using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using OpenTracing;

namespace DotNetWorkQueue.Trace.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IProducerMethodJobQueue" />
    public class ProducerMethodJobQueueDecorator: IProducerMethodJobQueue
    {
        private readonly ITracer _tracer;
        private readonly IProducerMethodJobQueue _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProducerMethodJobQueueDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        public ProducerMethodJobQueueDecorator(IProducerMethodJobQueue handler, ITracer tracer)
        {
            _handler = handler;
            _tracer = tracer;
        }

        /// <inheritdoc />
        public IJobSchedulerLastKnownEvent LastKnownEvent => _handler.LastKnownEvent;

        /// <inheritdoc />
        public QueueProducerConfiguration Configuration => _handler.Configuration;

        /// <inheritdoc />
        public ILog Logger => _handler.Logger;

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
            using (IScope scope = _tracer.BuildSpan("SendJobAsync").StartActive(finishSpanOnDispose: true))
            {
                scope.Span.SetTag("JobName", job.Name);
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
             using (IScope scope = _tracer.BuildSpan("SendJobAsync").StartActive(finishSpanOnDispose: true))
            {
                scope.Span.SetTag("JobName", job.Name);
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
