using DotNetWorkQueue.Dashboard.Api.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Models
{
    [TestClass]
    public class ErrorRetryResponseTests
    {
        [TestMethod]
        public void All_Properties_Can_Be_Set_And_Read()
        {
            var sut = new ErrorRetryResponse
            {
                ErrorTrackingId = 42L,
                QueueId = "msg-789",
                ExceptionType = "System.InvalidOperationException",
                RetryCount = 5
            };

            Assert.AreEqual(42L, sut.ErrorTrackingId);
            Assert.AreEqual("msg-789", sut.QueueId);
            Assert.AreEqual("System.InvalidOperationException", sut.ExceptionType);
            Assert.AreEqual(5, sut.RetryCount);
        }
    }
}
