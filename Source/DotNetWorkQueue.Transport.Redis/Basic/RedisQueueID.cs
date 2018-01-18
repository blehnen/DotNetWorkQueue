using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// An id for a redis message
    /// </summary>
    public class RedisQueueId: IMessageId
    {
        private readonly string _id;
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueId"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public RedisQueueId(string id)
        {
            _id = id;
            Id = new Setting<string>(id);
        }
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public ISetting Id { get; }
        /// <summary>
        /// Gets a value indicating if <see cref="Id" /> is not null / not empty
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Id" /> has value; otherwise, <c>false</c>.
        /// </value>
        public bool HasValue => !string.IsNullOrWhiteSpace(_id);

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _id;
        }
    }
}
