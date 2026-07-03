using DotNetWorkQueue.Dashboard.Api.Configuration;
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
            Assert.AreEqual("MyQueue", opts.QueueName);
        }

        [TestMethod]
        public void InterceptorConfiguration_Defaults_To_Null()
        {
            var opts = new DashboardQueueOptions();
            Assert.IsNull(opts.InterceptorConfiguration);
        }

        [TestMethod]
        public void InterceptorProfile_Defaults_To_Null()
        {
            var opts = new DashboardQueueOptions();
            Assert.IsNull(opts.InterceptorProfile);
        }

        [TestMethod]
        public void InterceptorProfile_Can_Be_Set()
        {
            var opts = new DashboardQueueOptions { InterceptorProfile = "encrypted" };
            Assert.AreEqual("encrypted", opts.InterceptorProfile);
        }

        [TestMethod]
        public void Interceptors_Defaults_To_Null()
        {
            var opts = new DashboardQueueOptions();
            Assert.IsNull(opts.Interceptors);
        }

        [TestMethod]
        public void Interceptors_Can_Be_Set()
        {
            var interceptors = new DashboardInterceptorOptions
            {
                GZip = new GZipInterceptorOptions()
            };
            var opts = new DashboardQueueOptions { Interceptors = interceptors };
            Assert.AreSame(interceptors, opts.Interceptors);
        }
    }
}
