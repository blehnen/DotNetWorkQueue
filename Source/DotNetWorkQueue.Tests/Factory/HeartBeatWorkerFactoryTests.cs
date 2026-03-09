using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using NSubstitute;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class HeartBeatWorkerFactoryTests
    {
        [TestMethod]
        public void Create_Disabled()
        {
            var factory = Create(false);
            var monitor = factory.Create(Substitute.For<IMessageContext>());
            Assert.IsInstanceOfType<INoOperation>(monitor);
        }

        private IHeartBeatWorkerFactory Create(bool enabled)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<IHeartBeatConfiguration>();
            configuration.Enabled.Returns(enabled);
            fixture.Inject(configuration);
            return fixture.Create<HeartBeatWorkerFactory>();
        }
    }
}
