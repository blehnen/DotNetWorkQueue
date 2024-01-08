using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Notifications;
using System;
using System.Collections.Generic;
using Xunit;

namespace DotNetWorkQueue.Tests.Notifications
{
    public class MessageCompleteNotificationTests
    {
        [Fact]
        public void Create_Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();
            var headers = new Dictionary<string, object>();
            var body = new Exception("none");
            var notify = new MessageCompleteNotification(messageId, correlationId, headers, body);

            Assert.NotNull(notify.Body);
            Assert.Equal(body, notify.Body);
            Assert.NotNull(notify.CorrelationId);
            Assert.NotNull(notify.Headers);
            Assert.NotNull(notify.MessageId);
            Assert.Null(notify.GetHeader<string>(new MessageContextData<string>("none", null)));
        }

        [Fact]
        public void CreateWithHeader_Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();
            var headers = new Dictionary<string, object> { { "one", "data" } };
            var body = new Exception("none");
            var notify = new MessageCompleteNotification(messageId, correlationId, headers, body);

            Assert.Null(notify.GetHeader<string>(new MessageContextData<string>("none", null)));
            Assert.Equal("data", notify.GetHeader(new MessageContextData<string>("one", null)));
        }
    }
}
