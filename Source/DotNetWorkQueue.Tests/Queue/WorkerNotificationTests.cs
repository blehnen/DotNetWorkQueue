using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerNotificationTests
    {
        [Fact]
        public void Rollback_Disabled_Default()
        {
            var test = Create();
            Assert.False(test.TransportSupportsRollback);
        }

        [Fact]
        public void Rollback_Enabled()
        {
            var test = Create(true);
            Assert.True(test.TransportSupportsRollback);
        }

        [Fact]
        public void WorkerStopping_NotNull()
        {
            var test = Create();
            Assert.NotNull(test.WorkerStopping);
        }

        [Fact]
        public void HeaderNames_NotNull()
        {
            var test = Create();
            Assert.NotNull(test.HeaderNames);
        }

        [Fact]  
        public void Log_NotNull()
        {
            var test = Create();
            Assert.NotNull(test.Log);
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
