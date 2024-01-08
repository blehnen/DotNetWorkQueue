using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Notifications;
using System;
using System.Collections.Generic;
using Xunit;

namespace DotNetWorkQueue.Tests.Notifications
{
    public class RollBackNotificationTests
    {
        [Fact]
        public void Create_Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();
            var headers = new Dictionary<string, object>();
            var body = new Exception("none");
            var notify = new RollBackNotification(messageId, correlationId, headers, body);

            Assert.NotNull(notify.Error);
            Assert.NotNull(notify.CorrelationId);
            Assert.NotNull(notify.Headers);
            Assert.NotNull(notify.MessageId);
            Assert.Null(notify.GetHeader<string>(new MessageContextData<string>("none", null)));
        }
    }
}
