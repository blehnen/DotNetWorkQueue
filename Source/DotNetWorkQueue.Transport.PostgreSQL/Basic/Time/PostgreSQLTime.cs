// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
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
        public PostgreSqlTime(ILogger log,
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
