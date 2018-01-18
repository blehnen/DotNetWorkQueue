using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Message
{
    /// <summary>
    /// Commits a processed message
    /// </summary>
    internal class CommitMessage
    {
        private readonly ICommandHandlerWithOutput<CommitMessageCommand, bool> _command;
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessage" /> class.
        /// </summary>
        /// <param name="command">The command.</param>
        public CommitMessage(ICommandHandlerWithOutput<CommitMessageCommand, bool> command)
        {
            Guard.NotNull(() => command, command);
            _command = command;
        }

        /// <summary>
        /// Commits the processed message, by deleting the message
        /// </summary>
        /// <param name="context">The context.</param>
        public void Commit(IMessageContext context)
        {
            if (context.MessageId == null || !context.MessageId.HasValue) return;
            _command.Handle(new CommitMessageCommand((RedisQueueId) context.MessageId));
        }
    }
}
