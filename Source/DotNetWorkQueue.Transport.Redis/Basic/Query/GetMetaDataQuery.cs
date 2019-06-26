using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Query
{
    /// <inheritdoc />
    /// <summary>
    /// Returns the meta data for a record
    /// </summary>
    public class GetMetaDataQuery : IQuery<RedisMetaData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetMetaDataQuery"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public GetMetaDataQuery(RedisQueueId id)
        {
            Guard.NotNull(() => id, id);
            Id = id;
        }
        /// <summary>
        /// Gets or sets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public RedisQueueId Id { get; }
    }
}
