using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Time;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Time
{
    /// <inheritdoc />
    internal class PostgreSqlTime: BaseTime
    {
        private readonly IQueryHandler<GetUtcDateQuery, DateTime> _queryHandler;
        private readonly IConnectionInformation _connectionInformation;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlTime" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="dateTimeQueryHandler">The date time query handler.</param>
        public PostgreSqlTime(ILogFactory log,
            BaseTimeConfiguration configuration,
            IConnectionInformation connectionInformation,
            IQueryHandler<GetUtcDateQuery, DateTime> dateTimeQueryHandler) : base(log, configuration)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => dateTimeQueryHandler, dateTimeQueryHandler);
            _queryHandler = dateTimeQueryHandler;
            _connectionInformation = connectionInformation;
        }

        /// <inheritdoc />
        public override string Name => "Postgre";

        /// <inheritdoc />
        protected override DateTime GetTime()
        {
            return _queryHandler.Handle(new GetUtcDateQuery(_connectionInformation.ConnectionString));
        }
    }
}
