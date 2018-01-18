using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Factory
{
    /// <inheritdoc />
    internal class ReceiveMessagesFactory : IReceiveMessagesFactory
    {
        private readonly IContainerFactory _container;
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessagesFactory" /> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public ReceiveMessagesFactory(IContainerFactory container)
        {
            Guard.NotNull(() => container, container);
            _container = container;
        }
        /// <inheritdoc />
        public IReceiveMessages Create()
        {
            return _container.Create().GetInstance<IReceiveMessages>();
        }
    }
}
