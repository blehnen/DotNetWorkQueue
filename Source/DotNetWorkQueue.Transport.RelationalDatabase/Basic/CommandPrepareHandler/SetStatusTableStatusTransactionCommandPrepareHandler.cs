using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    public class SetStatusTableStatusTransactionCommandPrepareHandler: IPrepareCommandHandler<SetStatusTableStatusTransactionCommand>
    {
        private readonly CommandStringCache _commandCache;

        public SetStatusTableStatusTransactionCommandPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }
        public void Handle(SetStatusTableStatusTransactionCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var queueId = dbCommand.CreateParameter();
            queueId.ParameterName = "@QueueID";
            queueId.DbType = DbType.Int64;
            queueId.Value = command.QueueId;
            dbCommand.Parameters.Add(queueId);

            var status = dbCommand.CreateParameter();
            status.ParameterName = "@Status";
            status.DbType = DbType.Int16;
            status.Value = Convert.ToInt16(command.Status);
            dbCommand.Parameters.Add(status);
        }
    }
}
