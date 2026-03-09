using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WorkerNotificationTests
    {
        [TestMethod]
        public void Rollback_Disabled_Default()
        {
            var test = Create();
            Assert.IsFalse(test.TransportSupportsRollback);
        }

        [TestMethod]
        public void Rollback_Enabled()
        {
            var test = Create(true);
            Assert.IsTrue(test.TransportSupportsRollback);
        }

        [TestMethod]
        public void WorkerStopping_NotNull()
        {
            var test = Create();
            Assert.IsNotNull(test.WorkerStopping);
        }

        [TestMethod]
        public void HeaderNames_NotNull()
        {
            var test = Create();
            Assert.IsNotNull(test.HeaderNames);
        }

        [TestMethod]
        public void Log_NotNull()
        {
            var test = Create();
            Assert.IsNotNull(test.Log);
        }

        private WorkerNotification Create(bool enableRollback = false)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<TransportConfigurationReceive>();
            configuration.MessageRollbackSupported = enableRollback;
            fixture.Inject(configuration);
            return fixture.Create<WorkerNotification>();
        }
    }
}
