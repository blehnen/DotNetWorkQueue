using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    public class ResetHeartBeatCommandPrepareHandler : IPrepareCommandHandler<ResetHeartBeatCommand>
    {
        private readonly CommandStringCache _commandCache;
        public ResetHeartBeatCommandPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        public void Handle(ResetHeartBeatCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(CommandStringTypes.ResetHeartbeat);

            var param = dbCommand.CreateParameter();
            param.ParameterName = "@QueueID";
            param.DbType = DbType.Int64;
            param.Value = command.MessageReset.QueueId;
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@SourceStatus";
            param.DbType = DbType.Int32;
            param.Value = Convert.ToInt16(QueueStatuses.Processing);
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@Status";
            param.DbType = DbType.Int32;
            param.Value = Convert.ToInt16(QueueStatuses.Waiting);
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@HeartBeat";
            param.DbType = DbType.DateTime2;
            param.Value = command.MessageReset.HeartBeat;
            dbCommand.Parameters.Add(param);
        }
    }
}
