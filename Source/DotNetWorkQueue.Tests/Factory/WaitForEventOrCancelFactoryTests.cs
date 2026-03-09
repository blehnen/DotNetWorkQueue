using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class WaitForEventOrCancelFactoryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = Create();
            Assert.IsNotNull(test.Create());
        }

        private IWaitForEventOrCancelFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WaitForEventOrCancelFactory>();
        }
    }
}
