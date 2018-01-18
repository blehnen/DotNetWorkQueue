using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.Message
{
    /// <summary>
    /// Commits a processed message
    /// </summary>
    internal class CommitMessage
    {
        private readonly ICommandHandlerWithOutput<DeleteMessageCommand, long> _deleteMessageCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitMessage" /> class.
        /// </summary>
        /// <param name="deleteMessageCommand">The delete message command.</param>
        public CommitMessage(
            ICommandHandlerWithOutput<DeleteMessageCommand, long> deleteMessageCommand)
        {
            Guard.NotNull(() => deleteMessageCommand, deleteMessageCommand);
            _deleteMessageCommand = deleteMessageCommand;
        }
        /// <summary>
        /// Commits the processed message, by deleting the message
        /// </summary>
        /// <param name="context">The context.</param>
        public void Commit(IMessageContext context)
        {
            if (context.MessageId != null && context.MessageId.HasValue)
            {
                _deleteMessageCommand.Handle(new DeleteMessageCommand((long)context.MessageId.Id.Value));
            }
        }
    }
}
