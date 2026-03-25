using DotNetWorkQueue.Dashboard.Api.Models;
using FluentAssertions;
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

            sut.ErrorTrackingId.Should().Be(42L);
            sut.QueueId.Should().Be("msg-789");
            sut.ExceptionType.Should().Be("System.InvalidOperationException");
            sut.RetryCount.Should().Be(5);
        }
    }
}
