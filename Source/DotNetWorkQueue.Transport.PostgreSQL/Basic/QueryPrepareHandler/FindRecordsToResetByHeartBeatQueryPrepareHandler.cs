using System;
using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryPrepareHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class FindRecordsToResetByHeartBeatQueryPrepareHandler : IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery, IEnumerable<MessageToReset>>
    {
        private readonly CommandStringCache _commandCache;
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IGetTime _getTime;
        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatQueryPrepareHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public FindRecordsToResetByHeartBeatQueryPrepareHandler(CommandStringCache commandCache,
            QueueConsumerConfiguration configuration,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            _commandCache = commandCache;
            _configuration = configuration;
            _getTime = getTimeFactory.Create();
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="dbCommand">The database command.</param>
        /// <param name="commandType">Type of the command.</param>
        public void Handle(FindMessagesToResetByHeartBeatQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText =
                _commandCache.GetCommand(commandType);

            var command = (NpgsqlCommand)dbCommand;

            command.Parameters.Add("@Time", NpgsqlDbType.Bigint);
            var selectTime = _getTime.GetCurrentUtcDate().AddSeconds(_configuration.HeartBeat.Time.TotalSeconds * -1);
            command.Parameters["@time"].Value = selectTime.Ticks;
            command.Parameters.Add("@Status", NpgsqlDbType.Integer);
            command.Parameters["@Status"].Value = Convert.ToInt16(QueueStatuses.Processing);

        }
    }
}
