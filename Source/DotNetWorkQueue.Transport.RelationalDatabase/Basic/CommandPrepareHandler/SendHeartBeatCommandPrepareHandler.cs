using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    public class SendHeartBeatCommandPrepareHandler: IPrepareCommandHandlerWithOutput<SendHeartBeatCommand, DateTime>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IGetTime _getTime;
        public SendHeartBeatCommandPrepareHandler(CommandStringCache commandCache,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            _commandCache = commandCache;
            _getTime = getTimeFactory.Create();
        }
        public DateTime Handle(SendHeartBeatCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var param = dbCommand.CreateParameter();
            param.ParameterName = "@QueueID";
            param.Value = command.QueueId;
            param.DbType = DbType.Int64;
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@Status";
            param.DbType = DbType.Int32;
            param.Value = Convert.ToInt16(QueueStatuses.Processing);
            dbCommand.Parameters.Add(param);

            param = dbCommand.CreateParameter();
            param.ParameterName = "@date";
            param.DbType = DbType.Int64;
            var date = _getTime.GetCurrentUtcDate();
            param.Value = date.Ticks;
            dbCommand.Parameters.Add(param);

            return date;
        }
    }
}
