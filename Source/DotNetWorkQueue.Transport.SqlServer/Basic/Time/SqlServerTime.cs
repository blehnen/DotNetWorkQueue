using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Time;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Time
{
    internal class SqlServerTime: BaseTime
    {
        private readonly IQueryHandler<GetUtcDateQuery, DateTime> _queryHandler;
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerTime" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="dateTimeQueryHandler">The date time query handler.</param>
        public SqlServerTime(ILogFactory log,
            BaseTimeConfiguration configuration,
            IConnectionInformation connectionInformation,
            IQueryHandler<GetUtcDateQuery, DateTime> dateTimeQueryHandler) : base(log, configuration)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => dateTimeQueryHandler, dateTimeQueryHandler);
            _queryHandler = dateTimeQueryHandler;
            _connectionInformation = connectionInformation;
        }

        /// <summary>
        /// Gets the name of the time provider
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "SQL";

        /// <summary>
        /// Gets the time.
        /// </summary>
        /// <returns></returns>
        protected override DateTime GetTime()
        {
            return _queryHandler.Handle(new GetUtcDateQuery(_connectionInformation.ConnectionString));
        }
    }
}
