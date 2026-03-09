using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using NSubstitute;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class ReceivedMessageInternalTests
    {
        [TestMethod]
        public void Create_Properties_Equals()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var message = fixture.Create<IMessage>();

            message.Body.Returns(new FakeMessage());
            message.Headers.Returns(new Dictionary<string, object>());

            message.Headers.Add(value, new UriBuilder());

            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();
            fixture.Inject(message);
            fixture.Inject(messageId);
            fixture.Inject(correlationId);
            var messageInternal = fixture.Create<ReceivedMessageInternal>();

            Assert.AreEqual(messageInternal.MessageId, messageId);
            Assert.AreEqual(messageInternal.Body, message.Body);
            Assert.AreEqual(messageInternal.CorrelationId, correlationId);
            CollectionAssert.AreEquivalent((System.Collections.ICollection)messageInternal.Headers, (System.Collections.ICollection)message.Headers);
        }

        private class FakeMessage
        {
        }
    }
}
