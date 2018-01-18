using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ITransportOptionsFactory" />
    public class TransportOptionsFactory : ITransportOptionsFactory
    {
        private readonly ISqLiteMessageQueueTransportOptionsFactory _factory;
        /// <summary>
        /// Initializes a new instance of the <see cref="TransportOptionsFactory"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public TransportOptionsFactory(ISqLiteMessageQueueTransportOptionsFactory factory)
        {
            Guard.NotNull(() => factory, factory);
            _factory = factory;
        }
        /// <summary>
        /// Returns the options class
        /// </summary>
        /// <returns></returns>
        public ITransportOptions Create()
        {
            return _factory.Create();
        }
    }
}
