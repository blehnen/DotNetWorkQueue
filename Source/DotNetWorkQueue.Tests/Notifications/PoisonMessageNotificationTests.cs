using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Notifications;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Notifications
{
    [TestClass]
    public class PoisonMessageNotificationTests
    {
        [TestMethod]
        public void Create_Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();
            var headers = new Dictionary<string, object>();
            var notify =
                new PoisonMessageNotification(new PoisonMessageException("error", messageId, correlationId, headers,
                    null, null));

            Assert.IsNotNull(notify.Error);
            Assert.IsNotNull(notify.CorrelationId);
            Assert.IsNotNull(notify.Headers);
            Assert.IsNotNull(notify.MessageId);
            Assert.IsNull(notify.GetHeader<string>(new MessageContextData<string>("none", null)));
        }
    }
}
