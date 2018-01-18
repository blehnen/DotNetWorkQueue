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
    public class MoveRecordToErrorQueueCommandPrepareHandler: IPrepareCommandHandler<MoveRecordToErrorQueueCommand>
    {
        private readonly IBuildMoveToErrorQueueSql _buildSql;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveRecordToErrorQueueCommandPrepareHandler" /> class.
        /// </summary>
        /// <param name="buildSql">The build SQL.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public MoveRecordToErrorQueueCommandPrepareHandler(IBuildMoveToErrorQueueSql buildSql,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => buildSql, buildSql);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            _buildSql = buildSql;
            _getTime = getTimeFactory.Create();
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="dbCommand">The database command.</param>
        /// <param name="commandType">Type of the command.</param>
        public void Handle(MoveRecordToErrorQueueCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _buildSql.Create();
            var commandSql = dbCommand;

            var param = commandSql.CreateParameter();
            param.ParameterName = "@QueueID";
            param.DbType = DbType.Int64;
            param.Value = command.QueueId;
            commandSql.Parameters.Add(param);

            param = commandSql.CreateParameter();
            param.ParameterName = "@LastException";
            param.DbType = DbType.String;
            param.Value = command.Exception.ToString();
            commandSql.Parameters.Add(param);

            param = commandSql.CreateParameter();
            param.ParameterName = "@CurrentDateTime";
            param.DbType = DbType.Int64;
            param.Value = _getTime.GetCurrentUtcDate().Ticks;
            commandSql.Parameters.Add(param);
        }
    }
}
