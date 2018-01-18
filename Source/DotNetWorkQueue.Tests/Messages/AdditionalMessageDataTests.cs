using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class AdditionalMessageDataTests
    {
        [Fact]
        public void SetAndGet_CorrelationId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();
            var correlationId = fixture.Create<ICorrelationId>();
            message.CorrelationId = correlationId;
            Assert.Equal(message.CorrelationId, correlationId);
        }

        [Fact]
        public void SetAndGet_Route()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();
            var route = fixture.Create<string>();
            message.Route = route;
            Assert.Equal(message.Route, route);
        }

        [Fact]
        public void SetAndGet_AdditionalMetaData()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();
            var messageData = fixture.Create<IAdditionalMetaData>();
            message.AdditionalMetaData.Add(messageData);
            Assert.Equal(message.AdditionalMetaData[0], messageData);
        }
        [Fact]
        public void SetAndGet_Headers()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();

            messageContextDataFactory.Create("Test", headerData)
                .Returns(new MessageContextData<HeaderData>("Test", headerData));

            var property = messageContextDataFactory.Create("Test", headerData);
            message.SetHeader(property, headerData);

            Assert.Equal(message.GetHeader(property), headerData);
        }

        [Fact]
        public void SetAndGet_Headers_RawAccess()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();

            messageContextDataFactory.Create("Test", headerData)
               .Returns(new MessageContextData<HeaderData>("Test", headerData));

            var property = messageContextDataFactory.Create("Test", headerData);
            message.SetHeader(property, headerData);

            Assert.Equal(message.Headers.Values.First(), headerData);
        }

        [Fact]
        public void GetSet_Headers_Default_Value()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<AdditionalMessageData>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            messageContextDataFactory.Create("Test", headerData).Returns(new MessageContextData<HeaderData>("Test", headerData));

            var property = messageContextDataFactory.Create("Test", headerData);
            var headerData2 = test.GetHeader(property);
            Assert.Equal(headerData2, headerData);

            var headerData3 = test.GetHeader(property);
            Assert.Equal(headerData2, headerData3);
        }

        public class HeaderData
        {
            
        }
    }
}
