using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    public class DeleteMessageCommandPrepareHandler : IPrepareCommandHandler<DeleteMessageCommand>
    {
        private readonly CommandStringCache _commandCache;

        public DeleteMessageCommandPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }
        public void Handle(DeleteMessageCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            //set ID if not set
            if (!dbCommand.Parameters.Contains("@QueueID"))
            {
                var param = dbCommand.CreateParameter();
                param.ParameterName = "@QueueID";
                param.DbType = DbType.Int64;
                param.Value = command.QueueId;
                dbCommand.Parameters.Add(param);
            }
            dbCommand.CommandText = _commandCache.GetCommand(commandType);
        }
    }
}
