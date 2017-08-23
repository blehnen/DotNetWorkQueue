using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    public class GetTableExistsQueryPrepareHandler : IPrepareQueryHandler<GetTableExistsQuery, bool>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;

        public GetTableExistsQueryPrepareHandler(CommandStringCache commandCache,
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
        }

        public void Handle(GetTableExistsQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var parameter = dbCommand.CreateParameter();
            parameter.ParameterName = "@Table";
            parameter.DbType = DbType.AnsiString;
            parameter.Value = query.TableName;
            dbCommand.Parameters.Add(parameter);

            var parameterDb = dbCommand.CreateParameter();
            parameterDb.ParameterName = "@Database";
            parameterDb.DbType = DbType.AnsiString;
            parameterDb.Value = _connectionInformation.Container;
            dbCommand.Parameters.Add(parameterDb);
        }
    }
}
