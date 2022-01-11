using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;



using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class ReceivedMessageTests
    {
        [Fact]
        public void Create_MessageId_Equals()
        {
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));
            Assert.Equal(test.MessageId, message.MessageId);
        }

        [Fact]
        public void Create_Body_Equals()
        {
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));
            Assert.Equal(test.Body, message.Body);
        }

        [Fact]
        public void Create_CorrelationId_Equals()
        {
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));
            Assert.Equal(test.CorrelationId, message.CorrelationId);
        }

        [Theory, AutoData]
        public void Create_Headers_Equals(string value)
        {
            var message = CreateMessage();

            var headers = new Dictionary<string, object> {{ value, new UriBuilder()}};

            message.Headers.Returns(new ReadOnlyDictionary<string, object>(headers));
            var test = new ReceivedMessage<FakeMessage>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));
            Assert.Equal(test.Headers, message.Headers);
        }

        [Theory, AutoData]
        public void GetHeader(string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var headerData = new HeaderData();
            messageContextDataFactory.Create(value, headerData)
                .Returns(new MessageContextData<HeaderData>(value, headerData));

            var property = messageContextDataFactory.Create(value, headerData);

            Assert.Equal(test.GetHeader(property), headerData);
        }

        private IReceivedMessageInternal CreateMessage()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<IReceivedMessageInternal>();
            message.Body.Returns(new FakeMessage());
            return message;
        }

        private class FakeMessage
        {
            
        }
        public class HeaderData 
        {
        }
    }
}
