namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// For redis, this class just indicates what is supported; Redis does not use a schema, so any option can be used at any time, even after queue creation.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IBaseTransportOptions" />
    public class RedisBaseTransportOptions: IBaseTransportOptions
    {
        /// <inheritdoc/>
        public bool EnablePriority => true;
        /// <inheritdoc/>
        public bool EnableStatus => true;
        /// <inheritdoc/>
        public bool EnableHeartBeat => true;
        /// <inheritdoc/>
        public bool EnableDelayedProcessing => true;
        /// <inheritdoc/>
        public bool EnableStatusTable => true;
        /// <inheritdoc/>
        public bool EnableRoute => true;
        /// <inheritdoc/>
        public bool EnableMessageExpiration => true;
    }
}
