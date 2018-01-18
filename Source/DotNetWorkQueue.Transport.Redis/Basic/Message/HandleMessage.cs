using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Message
{
    /// <summary>
    /// Rollback or commit messages
    /// </summary>
    internal class HandleMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HandleMessage"/> class.
        /// </summary>
        /// <param name="commitMessage">The commit message.</param>
        /// <param name="rollbackMessage">The rollback message.</param>
        public HandleMessage(CommitMessage commitMessage, 
            RollbackMessage rollbackMessage)
        {
            Guard.NotNull(() => commitMessage, commitMessage);
            Guard.NotNull(() => rollbackMessage, rollbackMessage);

            RollbackMessage = rollbackMessage;
            CommitMessage = commitMessage;
        }
        /// <summary>
        /// Gets the commit message module.
        /// </summary>
        /// <value>
        /// The commit message module.
        /// </value>
        public CommitMessage CommitMessage { get;  }
        /// <summary>
        /// Gets the rollback message module.
        /// </summary>
        /// <value>
        /// The rollback message module.
        /// </value>
        public RollbackMessage RollbackMessage { get; }
    }
}
