using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
    public class TransportOptionsFactory : ITransportOptionsFactory
    {
        private readonly IPostgreSqlMessageQueueTransportOptionsFactory _factory;
        /// <summary>
        /// Initializes a new instance of the <see cref="TransportOptionsFactory"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public TransportOptionsFactory(IPostgreSqlMessageQueueTransportOptionsFactory factory)
        {
            Guard.NotNull(() => factory, factory);
            _factory = factory;
        }
        /// <inheritdoc />
        public ITransportOptions Create()
        {
            return _factory.Create();
        }
    }
}
