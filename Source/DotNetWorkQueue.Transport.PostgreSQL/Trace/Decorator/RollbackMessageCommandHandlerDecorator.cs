using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using OpenTracing;

namespace DotNetWorkQueue.Transport.PostgreSQL.Trace.Decorator
{
    public class RollbackMessageCommandHandlerDecorator : ICommandHandler<RollbackMessageCommand>
    {
        private readonly ICommandHandler<RollbackMessageCommand> _handler;
        private readonly ITracer _tracer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandlerDecorator"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="tracer">The tracer.</param>
        public RollbackMessageCommandHandlerDecorator(ICommandHandler<RollbackMessageCommand> handler, ITracer tracer)
        {
            _handler = handler;
            _tracer = tracer;
        }

        /// <inheritdoc />
        public void Handle(RollbackMessageCommand command)
        {
            //lets add a bit more information to the active span if possible
            if (_tracer.ActiveSpan != null)
            {
                if (command.IncreaseQueueDelay.HasValue)
                    _tracer.ActiveSpan.SetTag("MessageDelay",
                        command.IncreaseQueueDelay.Value.ToString());

                if (command.LastHeartBeat.HasValue)
                    _tracer.ActiveSpan.SetTag("LastHeartBeatValue",
                        command.LastHeartBeat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            _handler.Handle(command);
        }
    }
}
