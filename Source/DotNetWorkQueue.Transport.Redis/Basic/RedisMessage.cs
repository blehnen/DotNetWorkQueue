namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// An internal class holding the result of a dequeue.
    /// </summary>
    internal class RedisMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisMessage" /> class.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="message">The message.</param>
        /// <param name="expired">if set to <c>true</c> [expired].</param>
        public RedisMessage(string messageId, IReceivedMessageInternal message, bool expired)
        {
            MessageId = messageId;
            Message = message;
            Expired = expired;
        }
        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        /// <remarks>This is the dequeued message</remarks>
        public IReceivedMessageInternal Message { get;}
        /// <summary>
        /// Gets a value indicating whether this <see cref="RedisMessage"/> is expired.
        /// </summary>
        /// <value>
        ///   <c>true</c> if expired; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>If the message has expired, it won't be processed; it will be deleted.</remarks>
        public bool Expired { get; }

        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        /// <remarks>Can be used to obtain the messageId of expired messages</remarks>
        public string MessageId { get;}
    }
}
