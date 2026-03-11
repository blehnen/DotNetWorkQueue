using DotNetWorkQueue.Dashboard.Api.Configuration;
using FluentAssertions;
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
            opts.GZip.Should().BeNull();
            opts.TripleDes.Should().BeNull();
        }

        [TestMethod]
        public void GZip_Defaults()
        {
            var opts = new GZipInterceptorOptions();
            opts.Enabled.Should().BeTrue();
            opts.MinimumSize.Should().Be(150);
        }

        [TestMethod]
        public void GZip_MinimumSize_Can_Be_Set()
        {
            var opts = new GZipInterceptorOptions { MinimumSize = 500 };
            opts.MinimumSize.Should().Be(500);
        }

        [TestMethod]
        public void TripleDes_Defaults()
        {
            var opts = new TripleDesInterceptorOptions();
            opts.Enabled.Should().BeTrue();
            opts.Key.Should().BeNull();
            opts.IV.Should().BeNull();
        }

        [TestMethod]
        public void TripleDes_Can_Be_Set()
        {
            var opts = new TripleDesInterceptorOptions
            {
                Key = "dGVzdGtleQ==",
                IV = "dGVzdGl2"
            };
            opts.Key.Should().Be("dGVzdGtleQ==");
            opts.IV.Should().Be("dGVzdGl2");
        }
    }
}
