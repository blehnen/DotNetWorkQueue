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

        [TestMethod]
        public void InterceptorProfile_Defaults_To_Null()
        {
            var opts = new DashboardQueueOptions();
            opts.InterceptorProfile.Should().BeNull();
        }

        [TestMethod]
        public void InterceptorProfile_Can_Be_Set()
        {
            var opts = new DashboardQueueOptions { InterceptorProfile = "encrypted" };
            opts.InterceptorProfile.Should().Be("encrypted");
        }

        [TestMethod]
        public void Interceptors_Defaults_To_Null()
        {
            var opts = new DashboardQueueOptions();
            opts.Interceptors.Should().BeNull();
        }

        [TestMethod]
        public void Interceptors_Can_Be_Set()
        {
            var interceptors = new DashboardInterceptorOptions
            {
                GZip = new GZipInterceptorOptions()
            };
            var opts = new DashboardQueueOptions { Interceptors = interceptors };
            opts.Interceptors.Should().BeSameAs(interceptors);
        }
    }
}
