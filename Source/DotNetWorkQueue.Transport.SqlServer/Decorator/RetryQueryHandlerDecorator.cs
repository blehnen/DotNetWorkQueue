using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Transport.SqlServer.Decorator
{
    /// <inheritdoc />
    internal class RetryQueryHandlerDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly IQueryHandler<TQuery, TResult> _decorated;
        private readonly IPolicies _policies;
        private ISyncPolicy _policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryQueryHandlerDecorator{TQuery,TResult}" /> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        /// <param name="policies">The policies.</param>
        public RetryQueryHandlerDecorator(IQueryHandler<TQuery, TResult> decorated, 
            IPolicies policies)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => policies, policies);

            _decorated = decorated;
            _policies = policies;
        }

        /// <inheritdoc />
        public TResult Handle(TQuery query)
        {
            Guard.NotNull(() => query, query);
            if (_policy == null)
            {
                _policies.Registry.TryGet(TransportPolicyDefinitions.RetryQueryHandler, out _policy);
            }
            if (_policy != null)
            {
                var result = _policy.ExecuteAndCapture(() => _decorated.Handle(query));
                if (result.FinalException != null)
                    throw result.FinalException;
                return result.Result;
            }
            return _decorated.Handle(query);
        }
    }
}
