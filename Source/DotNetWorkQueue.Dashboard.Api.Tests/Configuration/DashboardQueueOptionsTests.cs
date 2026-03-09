using DotNetWorkQueue.Dashboard.Api.Configuration;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Configuration
{
    [TestClass]
    public class DashboardQueueOptionsTests
    {
        [TestMethod]
        public void QueueName_Can_Be_Set()
        {
            var opts = new DashboardQueueOptions { QueueName = "MyQueue" };
            opts.QueueName.Should().Be("MyQueue");
        }

        [TestMethod]
        public void InterceptorConfiguration_Defaults_To_Null()
        {
            var opts = new DashboardQueueOptions();
            opts.InterceptorConfiguration.Should().BeNull();
        }
    }
}
