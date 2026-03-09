using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using NSubstitute;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class WorkerHeartBeatNotificationFactoryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var factory = Create(true);
            using (var source = new CancellationTokenSource())
            {
                Assert.IsNotNull(factory.Create(source.Token));
            }
        }

        [TestMethod]
        public void Create_NoOp()
        {
            var factory = Create(false);
            using (var source = new CancellationTokenSource())
            {
                var test = factory.Create(source.Token);
                Assert.IsInstanceOfType<INoOperation>(test);
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
