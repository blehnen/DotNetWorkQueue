using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Time;

namespace DotNetWorkQueue.Transport.Redis.Tests
{
    public static class Helpers
    {
        public static RedisQueueTransportOptions CreateOptions()
        {
            return new RedisQueueTransportOptions(new SntpTimeConfiguration(), new DelayedProcessingConfiguration());
        }
    }
}
