using System;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// A correlation Id that can be serialized
    /// </summary>
    public class RedisQueueCorrelationIdSerialized
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueCorrelationIdSerialized"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public RedisQueueCorrelationIdSerialized(Guid id)
        {
            Id = id;
        }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public Guid Id { get; set; }
    }

    /// <inheritdoc />
    public class RedisQueueCorrelationId: ICorrelationId
    {
        private Guid _id;
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueCorrelationId"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public RedisQueueCorrelationId(Guid id)
        {
            _id = id;
            Id = new Setting<Guid>(id);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueCorrelationId"/> class.
        /// </summary>
        /// <param name="input">The serialized input.</param>
        public RedisQueueCorrelationId(RedisQueueCorrelationIdSerialized input)
        {
            if(input != null)
            {
                _id = input.Id;
                Id = new Setting<Guid>(input.Id);
            }
            else
            {
                _id = Guid.Empty;
                Id = new Setting<Guid>(_id);
            }
        }
        /// <inheritdoc />
        public ISetting Id
        {
            get;
            set;
        }

        /// <inheritdoc />
        public bool HasValue => _id != Guid.Empty;

        /// <inheritdoc />
        public override string ToString()
        {
            return _id.ToString();
        }
    }
}
