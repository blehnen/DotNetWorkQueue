using DotNetWorkQueue.Dashboard.Api.Configuration;
using FluentAssertions;
using Xunit;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Configuration
{
    public class DashboardOptionsTests
    {
        [Fact]
        public void Defaults_Are_Correct()
        {
            var opts = new DashboardOptions();
            opts.EnableSwagger.Should().BeTrue();
            opts.AuthorizationPolicy.Should().BeNull();
        }

        [Fact]
        public void EnableSwagger_Can_Be_Disabled()
        {
            var opts = new DashboardOptions { EnableSwagger = false };
            opts.EnableSwagger.Should().BeFalse();
        }

        [Fact]
        public void AuthorizationPolicy_Can_Be_Set()
        {
            var opts = new DashboardOptions { AuthorizationPolicy = "AdminOnly" };
            opts.AuthorizationPolicy.Should().Be("AdminOnly");
        }
    }
}
