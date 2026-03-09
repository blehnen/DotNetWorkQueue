using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class ReceivedMessageTests
    {
        [TestMethod]
        public void Create_MessageId_Equals()
        {
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));
            Assert.AreEqual(test.MessageId, message.MessageId);
        }

        [TestMethod]
        public void Create_Body_Equals()
        {
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));
            Assert.AreEqual(test.Body, message.Body);
        }

        [TestMethod]
        public void Create_CorrelationId_Equals()
        {
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));
            Assert.AreEqual(test.CorrelationId, message.CorrelationId);
        }

        [TestMethod]
        public void Create_Headers_Equals()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var message = CreateMessage();

            var headers = new Dictionary<string, object> { { value, new UriBuilder() } };

            message.Headers.Returns(new ReadOnlyDictionary<string, object>(headers));
            var test = new ReceivedMessage<FakeMessage>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));
            CollectionAssert.AreEquivalent((System.Collections.ICollection)test.Headers, (System.Collections.ICollection)message.Headers);
        }

        [TestMethod]
        public void GetHeader()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message, new GetPreviousErrorsNoOp(), NullLoggerFactory.Instance.CreateLogger("null"));

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var headerData = new HeaderData();
            messageContextDataFactory.Create(value, headerData)
                .Returns(new MessageContextData<HeaderData>(value, headerData));

            var property = messageContextDataFactory.Create(value, headerData);

            Assert.AreEqual(test.GetHeader(property), headerData);
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
