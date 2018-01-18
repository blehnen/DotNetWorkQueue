using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.CommandPrepareHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class ResetHeartBeatCommandPrepareHandler : IPrepareCommandHandler<ResetHeartBeatCommand>
    {
        private readonly CommandStringCache _commandCache;
        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatCommandPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public ResetHeartBeatCommandPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="dbCommand">The database command.</param>
        /// <param name="commandType">Type of the command.</param>
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
            param.DbType = DbType.Int64;
            param.Value = command.MessageReset.HeartBeat.Ticks;
            dbCommand.Parameters.Add(param);
        }
    }
}
