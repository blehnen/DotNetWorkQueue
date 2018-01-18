using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class WorkerHeartBeatNotificationFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create(true);
            using (var source = new CancellationTokenSource())
            {
                Assert.NotNull(factory.Create(source.Token));
            }
        }

        [Fact]
        public void Create_NoOp()
        {
            var factory = Create(false);
            using (var source = new CancellationTokenSource())
            {
                var test = factory.Create(source.Token);
                Assert.IsAssignableFrom<INoOperation>(test);
            }
        }

        private IWorkerHeartBeatNotificationFactory Create(bool enabled)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<IHeartBeatConfiguration>();
            configuration.Enabled.Returns(enabled);
            fixture.Inject(configuration);
            return fixture.Create<WorkerHeartBeatNotificationFactory>();
        }
    }
}
