using System;
using System.Collections.Generic;

namespace DotNetWorkQueue.Queue
{
    public class GetHeaderDefault: IGetHeader
    {
        public IDictionary<string, object> GetHeaders(IMessageId id)
        {
            throw new DotNetWorkQueue.Exceptions.DotNetWorkQueueException("Transports must implement IGetHeader");
        }
    }
}
