using DotNetWorkQueue.Notifications;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Notifications
{
    [TestClass]
    public class ErrorReceiveNotificationTests
    {
        [TestMethod]
        public void Create_Test()
        {
            var notify = new ErrorReceiveNotification(new Exception());
            Assert.IsNotNull(notify.Error);
        }
    }
}
