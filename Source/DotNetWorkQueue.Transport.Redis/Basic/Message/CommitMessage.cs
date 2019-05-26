using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Message
{
    /// <summary>
    /// Commits a processed message
    /// </summary>
    internal class CommitMessage
    {
        private readonly IRemoveMessage _removeMessage;
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessage" /> class.
        /// </summary>
        public CommitMessage(IRemoveMessage removeMessage)
        {
            Guard.NotNull(() => removeMessage, removeMessage);
            _removeMessage = removeMessage;
        }

        /// <summary>
        /// Commits the processed message, by deleting the message
        /// </summary>
        /// <param name="context">The context.</param>
        public void Commit(IMessageContext context)
        {
            _removeMessage.Remove(context, RemoveMessageReason.Complete);
        }
    }
}
