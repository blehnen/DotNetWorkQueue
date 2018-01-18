using System.Collections.Generic;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Sends a new message to a queue
    /// </summary>
    public class SendMessageCommandBatch
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommand" /> class.
        /// </summary>
        /// <param name="messages">The messages.</param>
        public SendMessageCommandBatch(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
        {
            Guard.NotNull(() => messages, messages);
            Messages = messages;
        }
        /// <summary>
        /// Gets or sets the message to send.
        /// </summary>
        /// <value>
        /// The message to send.
        /// </value>
        public List<QueueMessage<IMessage, IAdditionalMessageData>> Messages { get; }
    }
}
