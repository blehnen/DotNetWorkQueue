using DotNetWorkQueue.Notifications;
using System;
using Xunit;

namespace DotNetWorkQueue.Tests.Notifications
{
    public class ErrorReceiveNotificationTests
    {
        [Fact]
        public void Create_Test()
        {
            var notify = new ErrorReceiveNotification(new Exception());
            Assert.NotNull(notify.Error);
        }
    }
}
