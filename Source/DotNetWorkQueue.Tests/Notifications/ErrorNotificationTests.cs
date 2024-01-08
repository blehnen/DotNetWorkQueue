using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Notifications;
using System;
using System.Collections.Generic;
using Xunit;

namespace DotNetWorkQueue.Tests.Notifications
{
    public class ErrorNotificationTests
    {
        [Fact]
        public void Create_Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();
            var headers = new Dictionary<string, object>();
            var notify = new ErrorNotification(messageId, correlationId, headers, new Exception());

            Assert.NotNull(notify.Error);
            Assert.NotNull(notify.CorrelationId);
            Assert.NotNull(notify.Headers);
            Assert.NotNull(notify.MessageId);
            Assert.Null(notify.GetHeader<string>(new MessageContextData<string>("none", null)));
        }
    }
}
