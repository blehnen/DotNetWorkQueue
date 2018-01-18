using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Command
{
    /// <summary>
    /// Sends a new message to a queue
    /// </summary>
    public class SendMessageCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommand"/> class.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="messageData">The message data.</param>
        public SendMessageCommand(IMessage messageToSend, IAdditionalMessageData messageData)
        {
            Guard.NotNull(() => messageToSend, messageToSend);
            Guard.NotNull(() => messageData, messageData);

            MessageData = messageData;
            MessageToSend = messageToSend;
        }
        /// <summary>
        /// Gets or sets the message to send.
        /// </summary>
        /// <value>
        /// The message to send.
        /// </value>
        public IMessage MessageToSend { get;  }
        /// <summary>
        /// Gets or sets the message data.
        /// </summary>
        /// <value>
        /// The message data.
        /// </value>
        public IAdditionalMessageData MessageData { get;  }
    }
}
