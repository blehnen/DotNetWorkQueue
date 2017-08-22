using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    public class SetErrorCountCommandPrepareHandler : IPrepareCommandHandler<SetErrorCountCommand>
    {
        private readonly CommandStringCache _commandCache;

        public SetErrorCountCommandPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }
        public void Handle(SetErrorCountCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            _commandCache.GetCommand(commandType);
            var param = dbCommand.CreateParameter();
            param.ParameterName = "@QueueID";
            param.DbType = DbType.Int64;
            param.Value = command.QueueId;
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@ExceptionType";
            param.DbType = DbType.AnsiStringFixedLength;
            param.Size = 500;
            param.Value = command.ExceptionType;
            dbCommand.Parameters.Add(param);
        }
    }
}
