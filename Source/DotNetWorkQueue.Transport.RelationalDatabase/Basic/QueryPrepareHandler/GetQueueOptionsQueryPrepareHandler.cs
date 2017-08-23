using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler
{
    public class GetQueueOptionsQueryPrepareHandler<TTransportOptions> : IPrepareQueryHandler<GetQueueOptionsQuery<TTransportOptions>, TTransportOptions>
        where TTransportOptions : class, ITransportOptions
    {
        private readonly CommandStringCache _commandCache;
        public GetQueueOptionsQueryPrepareHandler(CommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }
        public void Handle(GetQueueOptionsQuery<TTransportOptions> query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);
        }
    }
}
