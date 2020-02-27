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
using System.Collections.Generic;
using System.Text;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    internal class GetErrorRecordsToDeleteQueryHandler : IQueryHandler<GetErrorRecordsToDeleteQuery, List<string>>
    {
        private readonly IRedisConnection _connection;
        private readonly IUnixTimeFactory _unixTime;
        private readonly RedisNames _names;
        private readonly IMessageErrorConfiguration _errorConfiguration;
        private readonly RedisQueueTransportOptions _options;

        public GetErrorRecordsToDeleteQueryHandler(IRedisConnection connection,
            IUnixTimeFactory timeFactory,
            IMessageErrorConfiguration errorConfiguration,
            RedisNames names,
            RedisQueueTransportOptions options)
        {
            _connection = connection;
            _names = names;
            _errorConfiguration = errorConfiguration;
            _options = options;
            _unixTime = timeFactory;
        }

        public List<string> Handle(GetErrorRecordsToDeleteQuery query)
        {
            var returnData = new List<string>();
            if (_connection.IsDisposed)
                return returnData;

            var db = _connection.Connection.GetDatabase();
            var timeStamp = _unixTime.Create().GetSubtractDifferenceMilliseconds(_errorConfiguration.MessageAge);
            var results = db.SortedSetRangeByScore(_names.ErrorTime,
                double.NegativeInfinity, timeStamp, Exclude.None,
                Order.Descending, 0, _options.ClearErrorMessagesBatchLimit);

            foreach (var data in results)
            {
                returnData.Add(data);
            }

            return returnData;
        }
    }
}
