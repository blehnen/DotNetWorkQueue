using System;
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

        [TestMethod]
        public void AddInterceptorProfile_Stores_Profile()
        {
            var opts = new DashboardOptions();
            Action<IContainer> action = _ => { };
            opts.AddInterceptorProfile("encrypted", action);
            opts.InterceptorProfiles.Should().ContainKey("encrypted");
            opts.InterceptorProfiles["encrypted"].Should().BeSameAs(action);
        }

        [TestMethod]
        public void AddInterceptorProfile_Is_Case_Insensitive()
        {
            var opts = new DashboardOptions();
            Action<IContainer> action = _ => { };
            opts.AddInterceptorProfile("Encrypted", action);
            opts.InterceptorProfiles.Should().ContainKey("encrypted");
        }

        [TestMethod]
        public void AddInterceptorProfile_Overwrites_Existing()
        {
            var opts = new DashboardOptions();
            Action<IContainer> first = _ => { };
            Action<IContainer> second = _ => { };
            opts.AddInterceptorProfile("encrypted", first);
            opts.AddInterceptorProfile("encrypted", second);
            opts.InterceptorProfiles["encrypted"].Should().BeSameAs(second);
        }

        [TestMethod]
        public void AddInterceptorProfile_Throws_On_Null_Name()
        {
            var opts = new DashboardOptions();
            Action act = () => opts.AddInterceptorProfile(null, _ => { });
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AddInterceptorProfile_Throws_On_Empty_Name()
        {
            var opts = new DashboardOptions();
            Action act = () => opts.AddInterceptorProfile("", _ => { });
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AddInterceptorProfile_Throws_On_Null_Action()
        {
            var opts = new DashboardOptions();
            Action act = () => opts.AddInterceptorProfile("test", null);
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
