using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Messages;
using NSubstitute;



using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class CustomHeadersTests
    {
        [Theory, AutoData]
        public void Create_Default_GetSet(string name)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var testData = new TestData();

            var factory = fixture.Create<IMessageContextDataFactory>();
            factory.Create(name, testData).Returns(new MessageContextData<TestData>(name, testData));
            fixture.Inject(factory);
            var customHeaders = fixture.Create<CustomHeaders>();
            customHeaders.Add(name, testData);
            var data = customHeaders.Get<TestData>(name);

            Assert.Equal(testData, data.Default);
            Assert.Equal(name, data.Name);
        }

        public class TestData
        {

        }
    }
}
