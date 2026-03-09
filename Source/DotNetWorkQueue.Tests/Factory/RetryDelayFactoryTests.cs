using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class RetryDelayFactoryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = Create().Create();
            Assert.IsNotNull(test);
        }
        private IRetryDelayFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<RetryDelayFactory>();
        }
    }
}
