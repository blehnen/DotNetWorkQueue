using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class MoveRecordToErrorQueueCommandHandler : ICommandHandler<MoveRecordToErrorQueueCommand>
    {
        private readonly ErrorLua _errorLua;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="errorLua">The error lua.</param>
        public MoveRecordToErrorQueueCommandHandler(ErrorLua errorLua)
        {
            Guard.NotNull(() => errorLua, errorLua);
            _errorLua = errorLua;
        }

        /// <inheritdoc />
        public void Handle(MoveRecordToErrorQueueCommand command)
        {
            _errorLua.Execute(command.QueueId.Id.Value.ToString());
        }
    }
}
