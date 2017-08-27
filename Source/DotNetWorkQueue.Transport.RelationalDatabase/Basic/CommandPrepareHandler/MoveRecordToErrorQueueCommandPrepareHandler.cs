using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    public class MoveRecordToErrorQueueCommandPrepareHandler: IPrepareCommandHandler<MoveRecordToErrorQueueCommand>
    {
        private readonly IBuildMoveToErrorQueueSql _buildSql;

        public MoveRecordToErrorQueueCommandPrepareHandler(IBuildMoveToErrorQueueSql buildSql)
        {
            Guard.NotNull(() => buildSql, buildSql);
            _buildSql = buildSql;
        }
        public void Handle(MoveRecordToErrorQueueCommand command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _buildSql.Create();

            var queueId = dbCommand.CreateParameter();
            queueId.ParameterName = "@QueueID";
            queueId.DbType = DbType.Int64;
            queueId.Value = command.QueueId;
            dbCommand.Parameters.Add(queueId);

            var exception = dbCommand.CreateParameter();
            exception.ParameterName = "@LastException";
            exception.DbType = DbType.AnsiString;
            exception.Value = command.Exception.ToString();
            dbCommand.Parameters.Add(exception);
        }
    }
}
