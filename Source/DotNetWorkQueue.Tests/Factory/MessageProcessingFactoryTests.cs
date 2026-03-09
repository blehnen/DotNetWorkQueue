using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class MessageProcessingFactoryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var factory = Create();
            factory.Create();
        }

        private IMessageProcessingFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageProcessingFactory>();
        }
    }
}
