using System.Collections.Generic;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis
{
    internal class RedisGetPreviousMessageErrors: IGetPreviousMessageErrors
    {
        private readonly IQueryHandler<GetMetaDataQuery, RedisMetaData> _getMetaData;

        public RedisGetPreviousMessageErrors(IQueryHandler<GetMetaDataQuery, RedisMetaData> getMetaData)
        {
            Guard.NotNull(() => getMetaData, getMetaData);

            _getMetaData = getMetaData;
        }
        public IReadOnlyDictionary<string, int> Get(IMessageId id)
        {
            var metaData = _getMetaData.Handle(new GetMetaDataQuery((RedisQueueId)id));
            return metaData.ErrorTracking.Errors ?? new Dictionary<string, int>();
        }
    }
}
