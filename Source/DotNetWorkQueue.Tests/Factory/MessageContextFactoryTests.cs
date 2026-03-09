using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class MessageContextFactoryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var factory = Create();
            var test = factory.Create();
            Assert.IsNotNull(test);
        }
        private IMessageContextFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageContextFactory>();
        }
    }
}
