using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisQueueTransportOptionsTests
    {
        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = new RedisQueueTransportOptions(new SntpTimeConfiguration(),
                new DelayedProcessingConfiguration());
            Assert.False(configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = new RedisQueueTransportOptions(new SntpTimeConfiguration(),
               new DelayedProcessingConfiguration());
            configuration.SetReadOnly();
            Assert.True(configuration.IsReadOnly);
        }
        [Fact]
        public void Create_Default()
        {
            var sntpTime = new SntpTimeConfiguration();
            var delay = new DelayedProcessingConfiguration();
            var test = new RedisQueueTransportOptions(sntpTime,
                delay);
            Assert.Equal(sntpTime, test.SntpTimeConfiguration);
            Assert.Equal(delay, test.DelayedProcessingConfiguration);

            test.ClearExpiredMessagesBatchLimit = 1000;
            Assert.Equal(1000, test.ClearExpiredMessagesBatchLimit);

            test.MessageIdLocation = MessageIdLocations.Custom;
            Assert.Equal(MessageIdLocations.Custom, test.MessageIdLocation);

            test.MoveDelayedMessagesBatchLimit = 1000;
            Assert.Equal(1000, test.MoveDelayedMessagesBatchLimit);

            test.TimeServer = TimeLocations.Custom;
            Assert.Equal(TimeLocations.Custom, test.TimeServer);
        }
    }
}
