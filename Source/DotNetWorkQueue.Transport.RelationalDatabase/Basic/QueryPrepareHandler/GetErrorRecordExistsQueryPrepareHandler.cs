using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    public class GetErrorRecordExistsQueryPrepareHandler : IPrepareQueryHandler<GetErrorRecordExistsQuery, bool>
    {
        private readonly CommandStringCache _commandCache;
        public GetErrorRecordExistsQueryPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }
        public void Handle(GetErrorRecordExistsQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var queueid = dbCommand.CreateParameter();
            queueid.ParameterName = "@QueueID";
            queueid.DbType = DbType.Int64;
            queueid.Value = query.QueueId;
            dbCommand.Parameters.Add(queueid);

            var exceptionType = dbCommand.CreateParameter();
            exceptionType.ParameterName = "@ExceptionType";
            exceptionType.DbType = DbType.AnsiString;
            exceptionType.Size = 500;
            exceptionType.Value = query.ExceptionType;
            dbCommand.Parameters.Add(exceptionType);
        }
    }
}
