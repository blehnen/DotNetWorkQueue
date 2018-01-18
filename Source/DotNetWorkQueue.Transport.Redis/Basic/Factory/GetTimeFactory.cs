using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Factory
{
    /// <inheritdoc />
    internal class GetRedisTimeFactory : IGetTimeFactory
    {
        private readonly IUnixTimeFactory _unixTimeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRedisTimeFactory" /> class.
        /// </summary>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        public GetRedisTimeFactory(IUnixTimeFactory unixTimeFactory)
        {
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            _unixTimeFactory = unixTimeFactory;
        }
        /// <inheritdoc />
        public IGetTime Create()
        {
            return _unixTimeFactory.Create();
        }
    }
}
