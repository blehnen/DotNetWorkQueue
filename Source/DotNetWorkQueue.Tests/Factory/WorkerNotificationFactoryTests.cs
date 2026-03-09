using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class WorkerNotificationFactoryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var factory = Create();
            Assert.IsNotNull(factory.Create());
        }
        private IWorkerNotificationFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerNotificationFactory>();
        }
    }
}
