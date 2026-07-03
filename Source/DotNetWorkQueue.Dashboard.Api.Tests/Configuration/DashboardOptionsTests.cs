using System;
using System.Linq;
using DotNetWorkQueue.Dashboard.Api.Configuration;
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
            Assert.IsTrue(opts.EnableSwagger);
            Assert.IsNull(opts.AuthorizationPolicy);
        }

        [TestMethod]
        public void EnableSwagger_Can_Be_Disabled()
        {
            var opts = new DashboardOptions { EnableSwagger = false };
            Assert.IsFalse(opts.EnableSwagger);
        }

        [TestMethod]
        public void AuthorizationPolicy_Can_Be_Set()
        {
            var opts = new DashboardOptions { AuthorizationPolicy = "AdminOnly" };
            Assert.AreEqual("AdminOnly", opts.AuthorizationPolicy);
        }

        [TestMethod]
        public void AddInterceptorProfile_Stores_Profile()
        {
            var opts = new DashboardOptions();
            Action<IContainer> action = _ => { };
            opts.AddInterceptorProfile("encrypted", action);
            Assert.IsTrue((opts.InterceptorProfiles).ContainsKey("encrypted"));
            Assert.AreSame(action, opts.InterceptorProfiles["encrypted"]);
        }

        [TestMethod]
        public void AddInterceptorProfile_Is_Case_Insensitive()
        {
            var opts = new DashboardOptions();
            Action<IContainer> action = _ => { };
            opts.AddInterceptorProfile("Encrypted", action);
            Assert.IsTrue((opts.InterceptorProfiles).ContainsKey("encrypted"));
        }

        [TestMethod]
        public void AddInterceptorProfile_Overwrites_Existing()
        {
            var opts = new DashboardOptions();
            Action<IContainer> first = _ => { };
            Action<IContainer> second = _ => { };
            opts.AddInterceptorProfile("encrypted", first);
            opts.AddInterceptorProfile("encrypted", second);
            Assert.AreSame(second, opts.InterceptorProfiles["encrypted"]);
        }

        [TestMethod]
        public void AddInterceptorProfile_Throws_On_Null_Name()
        {
            var opts = new DashboardOptions();
            Action act = () => opts.AddInterceptorProfile(null, _ => { });
            Assert.Throws<ArgumentException>(act);
        }

        [TestMethod]
        public void AddInterceptorProfile_Throws_On_Empty_Name()
        {
            var opts = new DashboardOptions();
            Action act = () => opts.AddInterceptorProfile("", _ => { });
            Assert.Throws<ArgumentException>(act);
        }

        [TestMethod]
        public void AddInterceptorProfile_Throws_On_Null_Action()
        {
            var opts = new DashboardOptions();
            Action act = () => opts.AddInterceptorProfile("test", null);
            Assert.Throws<ArgumentNullException>(act);
        }

        [TestMethod]
        public void EnableCors_Defaults_To_False()
        {
            var opts = new DashboardOptions();
            Assert.IsFalse(opts.EnableCors);
        }

        [TestMethod]
        public void CorsOrigins_Defaults_To_Empty()
        {
            var opts = new DashboardOptions();
            Assert.IsFalse((opts.CorsOrigins).Any());
        }

        [TestMethod]
        public void CorsOrigins_Can_Be_Set()
        {
            var opts = new DashboardOptions
            {
                EnableCors = true,
                CorsOrigins = new[] { "http://localhost:5000" }
            };
            Assert.IsTrue(opts.EnableCors);
            Assert.AreEqual(1, (opts.CorsOrigins).Count());
            Assert.AreEqual("http://localhost:5000", (opts.CorsOrigins).Single());
        }

        [TestMethod]
        public void AssemblyPaths_Defaults_To_Empty()
        {
            var opts = new DashboardOptions();
            Assert.IsFalse((opts.AssemblyPaths).Any());
        }

        [TestMethod]
        public void AssemblyPaths_Can_Be_Set()
        {
            var opts = new DashboardOptions
            {
                AssemblyPaths = new[] { "/app/plugins", "/opt/dlls" }
            };
            Assert.AreEqual(2, (opts.AssemblyPaths).Count());
            Assert.AreEqual("/app/plugins", opts.AssemblyPaths[0]);
            Assert.AreEqual("/opt/dlls", opts.AssemblyPaths[1]);
        }
    }
}
