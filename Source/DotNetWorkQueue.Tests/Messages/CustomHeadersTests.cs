using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using NSubstitute;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class CustomHeadersTests
    {
        [TestMethod]
        public void Create_Default_GetSet()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();

            var testData = new TestData();

            var factory = fixture.Create<IMessageContextDataFactory>();
            factory.Create(name, testData).Returns(new MessageContextData<TestData>(name, testData));
            fixture.Inject(factory);
            var customHeaders = fixture.Create<CustomHeaders>();
            customHeaders.Add(name, testData);
            var data = customHeaders.Get<TestData>(name);

            Assert.AreEqual(testData, data.Default);
            Assert.AreEqual(name, data.Name);
        }

        [TestClass]

        public class TestData
        {

        }
    }
}
