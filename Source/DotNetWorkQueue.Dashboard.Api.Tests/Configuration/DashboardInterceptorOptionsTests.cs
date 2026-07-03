using DotNetWorkQueue.Dashboard.Api.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Configuration
{
    [TestClass]
    public class DashboardInterceptorOptionsTests
    {
        [TestMethod]
        public void Defaults_Are_Null()
        {
            var opts = new DashboardInterceptorOptions();
            Assert.IsNull(opts.GZip);
            Assert.IsNull(opts.TripleDes);
        }

        [TestMethod]
        public void GZip_Defaults()
        {
            var opts = new GZipInterceptorOptions();
            Assert.IsTrue(opts.Enabled);
            Assert.AreEqual(150, opts.MinimumSize);
        }

        [TestMethod]
        public void GZip_MinimumSize_Can_Be_Set()
        {
            var opts = new GZipInterceptorOptions { MinimumSize = 500 };
            Assert.AreEqual(500, opts.MinimumSize);
        }

        [TestMethod]
        public void TripleDes_Defaults()
        {
            var opts = new TripleDesInterceptorOptions();
            Assert.IsTrue(opts.Enabled);
            Assert.IsNull(opts.Key);
            Assert.IsNull(opts.IV);
        }

        [TestMethod]
        public void TripleDes_Can_Be_Set()
        {
            var opts = new TripleDesInterceptorOptions
            {
                Key = "dGVzdGtleQ==",
                IV = "dGVzdGl2"
            };
            Assert.AreEqual("dGVzdGtleQ==", opts.Key);
            Assert.AreEqual("dGVzdGl2", opts.IV);
        }
    }
}
