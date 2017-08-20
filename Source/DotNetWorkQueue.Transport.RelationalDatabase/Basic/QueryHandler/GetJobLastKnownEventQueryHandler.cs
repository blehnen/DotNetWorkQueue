// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class GetJobLastKnownEventQueryHandler : IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>
    {
        private readonly CommandStringCache _commandCache;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IDateTimeOffsetParser _dateTimeOffsetParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetJobLastKnownEventQueryHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="dbConnectionFactory">The database connection factory.</param>
        /// <param name="dateTimeOffsetParser">The date time offset parser.</param>
        public GetJobLastKnownEventQueryHandler(CommandStringCache commandCache,
            IDbConnectionFactory dbConnectionFactory,
            IDateTimeOffsetParser dateTimeOffsetParser)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => dateTimeOffsetParser, dateTimeOffsetParser);

            _commandCache = commandCache;
            _dbConnectionFactory = dbConnectionFactory;
            _dateTimeOffsetParser = dateTimeOffsetParser;
        }
        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public DateTimeOffset Handle(GetJobLastKnownEventQuery query)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _commandCache.GetCommand(CommandStringTypes.GetJobLastKnownEvent);

                    var param = command.CreateParameter();
                    param.ParameterName = "@JobName";
                    param.Size = 255;
                    param.DbType = DbType.AnsiString;
                    param.Value = query.JobName;
                    command.Parameters.Add(param);

                    using (var reader = command.ExecuteReader())
                    {
                        return !reader.Read() ? default(DateTimeOffset) : _dateTimeOffsetParser.Parse(reader[0]);
                    }
                }
            }
        }
    }
}
