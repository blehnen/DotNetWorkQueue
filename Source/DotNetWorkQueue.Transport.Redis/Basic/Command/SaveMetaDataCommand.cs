using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Saves the meta data - either a new record or an update to an existing one.
    /// </summary>
    public class SaveMetaDataCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveMetaDataCommand" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="metaData">The meta data.</param>
        public SaveMetaDataCommand(RedisQueueId id, RedisMetaData metaData)
        {
            Guard.NotNull(() => id, id);
            Guard.NotNull(() => metaData, metaData);
            Id = id;
            MetaData = metaData;
        }
        /// <summary>
        /// Gets the meta data.
        /// </summary>
        /// <value>
        /// The meta data.
        /// </value>
        public RedisMetaData MetaData { get; }
        /// <summary>
        /// Gets or sets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public RedisQueueId Id { get;}
    }
}
