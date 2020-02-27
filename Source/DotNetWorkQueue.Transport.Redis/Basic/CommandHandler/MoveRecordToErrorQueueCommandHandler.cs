using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class MoveRecordToErrorQueueCommandHandler : ICommandHandler<MoveRecordToErrorQueueCommand>
    {
        private readonly ErrorLua _errorLua;
        private readonly IUnixTime _unixTime;

        /// <summary>Initializes a new instance of the <see cref="DeleteMessageCommandHandler"/> class.</summary>
        /// <param name="errorLua">The error lua.</param>
        /// <param name="timeFactory">Time factory</param>
        public MoveRecordToErrorQueueCommandHandler(ErrorLua errorLua, IUnixTimeFactory timeFactory)
        {
            Guard.NotNull(() => errorLua, errorLua);
            Guard.NotNull(() => timeFactory, timeFactory);
            _errorLua = errorLua;
            _unixTime = timeFactory.Create();
        }

        /// <inheritdoc />
        public void Handle(MoveRecordToErrorQueueCommand command)
        {
            _errorLua.Execute(command.QueueId.Id.Value.ToString(), _unixTime.GetCurrentUnixTimestampMilliseconds());
        }
    }
}
