using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class WorkerNotificationFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create();
            Assert.NotNull(factory.Create());
        }
        private IWorkerNotificationFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerNotificationFactory>();
        }
    }
}
