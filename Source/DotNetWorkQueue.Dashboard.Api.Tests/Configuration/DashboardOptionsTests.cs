using DotNetWorkQueue.Dashboard.Api.Configuration;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Configuration
{
    [TestClass]
    public class DashboardOptionsTests
    {
        [TestMethod]
        public void Defaults_Are_Correct()
        {
            var opts = new DashboardOptions();
            opts.EnableSwagger.Should().BeTrue();
            opts.AuthorizationPolicy.Should().BeNull();
        }

        [TestMethod]
        public void EnableSwagger_Can_Be_Disabled()
        {
            var opts = new DashboardOptions { EnableSwagger = false };
            opts.EnableSwagger.Should().BeFalse();
        }

        [TestMethod]
        public void AuthorizationPolicy_Can_Be_Set()
        {
            var opts = new DashboardOptions { AuthorizationPolicy = "AdminOnly" };
            opts.AuthorizationPolicy.Should().Be("AdminOnly");
        }
    }
}
