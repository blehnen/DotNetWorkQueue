using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class HeadersTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var headers = fixture.Create<IStandardHeaders>();
            var customHeaders = fixture.Create<ICustomHeaders>();
            fixture.Inject(headers);
            fixture.Inject(customHeaders);
            var test = fixture.Create<Headers>();

            Assert.AreEqual(headers, test.StandardHeaders);
            Assert.AreEqual(customHeaders, test.CustomHeaders);
        }
    }
}
