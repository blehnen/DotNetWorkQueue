using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisQueueTransportOptionsTests
    {
        [TestMethod]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = new RedisQueueTransportOptions(new SntpTimeConfiguration(),
                new DelayedProcessingConfiguration());
            Assert.IsFalse(configuration.IsReadOnly);
        }
        [TestMethod]
        public void Set_Readonly()
        {
            var configuration = new RedisQueueTransportOptions(new SntpTimeConfiguration(),
               new DelayedProcessingConfiguration());
            configuration.SetReadOnly();
            Assert.IsTrue(configuration.IsReadOnly);
        }
        [TestMethod]
        public void Create_Default()
        {
            var sntpTime = new SntpTimeConfiguration();
            var delay = new DelayedProcessingConfiguration();
            var test = new RedisQueueTransportOptions(sntpTime,
                delay);
            Assert.AreEqual(sntpTime, test.SntpTimeConfiguration);
            Assert.AreEqual(delay, test.DelayedProcessingConfiguration);

            test.ClearExpiredMessagesBatchLimit = 1000;
            Assert.AreEqual(1000, test.ClearExpiredMessagesBatchLimit);

            test.MessageIdLocation = MessageIdLocations.Custom;
            Assert.AreEqual(MessageIdLocations.Custom, test.MessageIdLocation);

            test.MoveDelayedMessagesBatchLimit = 1000;
            Assert.AreEqual(1000, test.MoveDelayedMessagesBatchLimit);

            test.TimeServer = TimeLocations.Custom;
            Assert.AreEqual(TimeLocations.Custom, test.TimeServer);
        }
    }
}
