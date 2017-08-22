using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    public class DeleteStatusTableStatusCommandPrepareHandler: IPrepareCommandHandler<DeleteStatusTableStatusCommand>
    {
        private readonly CommandStringCache _commandCache;
        public DeleteStatusTableStatusCommandPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        public void Handle(DeleteStatusTableStatusCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(CommandStringTypes.DeleteFromStatus);
            var param = dbCommand.CreateParameter();
            param.ParameterName = "@QueueID";
            param.Value = command.QueueId;
            param.DbType = DbType.Int64;
            dbCommand.Parameters.Add(param);
        }
    }
}
