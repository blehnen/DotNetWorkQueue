using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.QueryPrepareHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class FindExpiredRecordsToDeleteQueryPrepareHandler : IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery, IEnumerable<long>>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpiredRecordsToDeleteQueryPrepareHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="timeFactory">The time factory.</param>
        public FindExpiredRecordsToDeleteQueryPrepareHandler(CommandStringCache commandCache,
            IGetTimeFactory timeFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => timeFactory, timeFactory);
            _commandCache = commandCache;
            _getTime = timeFactory.Create();
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="dbCommand">The database command.</param>
        /// <param name="commandType">Type of the command.</param>
        public void Handle(FindExpiredMessagesToDeleteQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var command = dbCommand;
            var param = command.CreateParameter();
            param.ParameterName = "@CurrentDateTime";
            param.DbType = DbType.Int64;
            param.Value = _getTime.GetCurrentUtcDate().Ticks;
            command.Parameters.Add(param);
        }
    }
}
