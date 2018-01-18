using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Factory;



using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class MessageFactoryTests
    {
        [Theory, AutoData]
        public void Create_Message(Data data, Dictionary<string, object> headers)
        {
            var factory = Create();
            var test = factory.Create(data, headers);
            Assert.Equal(test.Body, data);
            Assert.Equal(test.Headers, headers);
        }

        [Theory, AutoData]
        public void GetSet_Header(Data data, Dictionary<string, object> headers, HeaderData headerData)
        {
            var factory = Create();
            var test = factory.Create(data, headers);

            var messageContextDataFactory = CreateDataFactory();

            var property = messageContextDataFactory.Create("Test", headerData);
            test.SetHeader(property, headerData);

            var headerData2 = test.GetHeader(property);
            Assert.Equal(headerData2, headerData);
        }
        [Theory, AutoData]
        public void GetSet_InternalHeader(Data data, Dictionary<string, object> headers, HeaderData headerData)
        {
            var factory = Create();
            var test = factory.Create(data, headers);

            var messageContextDataFactory = CreateDataFactory();

            var property = messageContextDataFactory.Create("Test", headerData);
            test.SetInternalHeader(property, headerData);

            var headerData2 = test.GetInternalHeader(property);
            Assert.Equal(headerData2, headerData);
        }

        private IMessageFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageFactory>();
        }

        private IMessageContextDataFactory CreateDataFactory()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<IMessageContextDataFactory>();
        }
        public class Data
        {

        }
        public class HeaderData
        { }
    }
}
