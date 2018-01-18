using System.Data;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Validation;
using Polly;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Decorator
{
    /// <inheritdoc />
    internal class BeginTransactionRetryDecorator : ISQLiteTransactionWrapper
    {
        private readonly ISQLiteTransactionWrapper _decorated;
        private readonly IPolicies _policies;
        private Policy _policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="BeginTransactionRetryDecorator"/> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        /// <param name="policies">The policies.</param>
        public BeginTransactionRetryDecorator(ISQLiteTransactionWrapper decorated,
            IPolicies policies)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => policies, policies);

            _decorated = decorated;
            _policies = policies;
        }

        /// <inheritdoc />
        public IDbConnection Connection
        {
            get => _decorated.Connection;
            set => _decorated.Connection = value;
        }

        /// <inheritdoc />
        public IDbTransaction BeginTransaction()
        {
            if (_policy == null)
            {
                _policies.Registry.TryGet(TransportPolicyDefinitions.BeginTransaction, out _policy);
            }
            if (_policy == null) return _decorated.BeginTransaction();
            var result = _policy.ExecuteAndCapture(() => _decorated.BeginTransaction());
            if (result.FinalException != null)
                throw result.FinalException;
            return result.Result;
        }
    }
}
