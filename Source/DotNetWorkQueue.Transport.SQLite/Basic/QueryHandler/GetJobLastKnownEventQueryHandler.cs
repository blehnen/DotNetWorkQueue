// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Data;
using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class GetJobLastKnownEventQueryHandler : IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>
    {
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetJobLastKnownEventQueryHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public GetJobLastKnownEventQueryHandler(SqLiteCommandStringCache commandCache,
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public DateTimeOffset Handle(GetJobLastKnownEventQuery query)
        {
            using (var connection = new SQLiteConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.GetJobLastKnownEvent);

                    command.Parameters.Add("@JobName", DbType.String, 255);
                    command.Parameters["@JobName"].Value = query.JobName;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var data = reader.GetString(0);
                            return DateTimeOffset.Parse(data, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
            return default(DateTimeOffset);
        }
    }
}
