using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class DeleteMessageCommandHandler : ICommandHandlerWithOutput<DeleteMessageCommand, bool>
    {
        private readonly DeleteLua _deleteLua;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="deleteLua">The delete lua.</param>
        public DeleteMessageCommandHandler(DeleteLua deleteLua)
        {
            Guard.NotNull(() => deleteLua, deleteLua);
            _deleteLua = deleteLua;
        }

        /// <inheritdoc />
        public bool Handle(DeleteMessageCommand command)
        {
            var result = _deleteLua.Execute(command.Id.Id.Value.ToString());
            return result.HasValue && result.Value == 1;
        }
    }
}
