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
