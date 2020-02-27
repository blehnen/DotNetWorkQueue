using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.QueryPrepareHandler
{
    public class FindErrorRecordsToDeleteQueryPrepareHandler : IPrepareQueryHandler<FindErrorMessagesToDeleteQuery, IEnumerable<long>>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IMessageErrorConfiguration _configuration;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindErrorRecordsToDeleteQueryPrepareHandler"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="timeFactory">The time factory.</param>
        /// <param name="configuration">The configuration.</param>
        public FindErrorRecordsToDeleteQueryPrepareHandler(CommandStringCache commandCache,
            IGetTimeFactory timeFactory,
            IMessageErrorConfiguration configuration)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => timeFactory, timeFactory);
            _commandCache = commandCache;
            _configuration = configuration;
            _getTime = timeFactory.Create();
        }
        /// <inheritdoc />
        public void Handle(FindErrorMessagesToDeleteQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _commandCache.GetCommand(commandType);

            var command = dbCommand;
            var param = command.CreateParameter();
            param.ParameterName = "@CurrentDateTime";
            param.DbType = DbType.Int64;
            param.Value = _getTime.GetCurrentUtcDate().Ticks + _configuration.MessageAge.Ticks;
            command.Parameters.Add(param);
        }
    }
}
