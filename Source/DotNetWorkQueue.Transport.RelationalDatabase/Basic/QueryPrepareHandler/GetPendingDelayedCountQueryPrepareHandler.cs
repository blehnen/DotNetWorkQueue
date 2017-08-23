using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    public class GetPendingDelayedCountQueryPrepareHandler : IPrepareQueryHandler<GetPendingDelayedCountQuery, long>
    {
        private readonly CommandStringCache _commandCache;
        public GetPendingDelayedCountQueryPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        public void Handle(GetPendingDelayedCountQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);
        }
    }
}
