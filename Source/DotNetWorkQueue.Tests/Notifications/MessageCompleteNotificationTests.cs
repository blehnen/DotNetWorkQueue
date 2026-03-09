using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Notifications;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Notifications
{
    [TestClass]
    public class MessageCompleteNotificationTests
    {
        [TestMethod]
        public void Create_Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();
            var headers = new Dictionary<string, object>();
            var body = new Exception("none");
            var notify = new MessageCompleteNotification(messageId, correlationId, headers, body);

            Assert.IsNotNull(notify.Body);
            Assert.AreEqual(body, notify.Body);
            Assert.IsNotNull(notify.CorrelationId);
            Assert.IsNotNull(notify.Headers);
            Assert.IsNotNull(notify.MessageId);
            Assert.IsNull(notify.GetHeader<string>(new MessageContextData<string>("none", null)));
        }

        [TestMethod]
        public void CreateWithHeader_Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();
            var headers = new Dictionary<string, object> { { "one", "data" } };
            var body = new Exception("none");
            var notify = new MessageCompleteNotification(messageId, correlationId, headers, body);

            Assert.IsNull(notify.GetHeader<string>(new MessageContextData<string>("none", null)));
            Assert.AreEqual("data", notify.GetHeader(new MessageContextData<string>("one", null)));
        }
    }
}
