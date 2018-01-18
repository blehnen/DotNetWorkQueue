using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class CommitMessageCommandHandler : ICommandHandlerWithOutput<CommitMessageCommand, bool>
    {
        private readonly CommitLua _commitLua;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="commitLua">The delete lua.</param>
        public CommitMessageCommandHandler(CommitLua commitLua)
        {
            Guard.NotNull(() => commitLua, commitLua);
            _commitLua = commitLua;
        }

        /// <inheritdoc />
        public bool Handle(CommitMessageCommand command)
        {
            var result = _commitLua.Execute(command.Id.Id.Value.ToString());
            return result.HasValue && result.Value == 1;
        }
    }
}
