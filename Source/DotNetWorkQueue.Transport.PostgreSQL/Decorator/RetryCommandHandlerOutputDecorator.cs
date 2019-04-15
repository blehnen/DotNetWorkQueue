using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    /// <inheritdoc />
    internal class RetryCommandHandlerOutputDecorator<TCommand, TOutput> : ICommandHandlerWithOutput<TCommand, TOutput>
    {
        private readonly ICommandHandlerWithOutput<TCommand, TOutput> _decorated;
        private readonly IPolicies _policies;
        private ISyncPolicy _policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryCommandHandlerOutputDecorator{TCommand,TOutput}" /> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        /// <param name="policies">The policies.</param>
        public RetryCommandHandlerOutputDecorator(ICommandHandlerWithOutput<TCommand, TOutput> decorated,
            IPolicies policies)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => policies, policies);

            _decorated = decorated;
            _policies = policies;
        }

        /// <inheritdoc />
        public TOutput Handle(TCommand command)
        {
            Guard.NotNull(() => command, command);

            if (_policy == null)
            {
                _policies.Registry.TryGet(TransportPolicyDefinitions.RetryCommandHandler, out _policy);
            }
            if (_policy == null) return _decorated.Handle(command);
            var result = _policy.ExecuteAndCapture(() => _decorated.Handle(command));
            if (result.FinalException != null)
                throw result.FinalException;
            return result.Result;
        }
    }
}
