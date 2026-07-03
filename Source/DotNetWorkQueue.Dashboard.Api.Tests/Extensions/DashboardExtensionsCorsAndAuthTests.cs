// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetWorkQueue.Dashboard.Api;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using DotNetWorkQueue.Dashboard.Api.Controllers;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Extensions
{
    [TestClass]
    public class DashboardExtensionsCorsAndAuthTests
    {
        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Registers_CorsPolicy_When_Enabled_With_Origins()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
                options.EnableCors = true;
                options.CorsOrigins = new[] { "https://example.com", "https://localhost:5001" };
            });

            var provider = services.BuildServiceProvider();
            var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;

            var policy = corsOptions.GetPolicy("DashboardCors");
            Assert.IsNotNull(policy);
            CollectionAssert.AreEquivalent((System.Collections.ICollection)new[] { "https://example.com", "https://localhost:5001" }, (System.Collections.ICollection)(policy!.Origins));
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Does_Not_Register_CorsPolicy_When_Origins_Empty()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
                options.EnableCors = true;
                options.CorsOrigins = Array.Empty<string>();
            });

            var provider = services.BuildServiceProvider();
            var corsOptions = provider.GetService<IOptions<CorsOptions>>();

            if (corsOptions != null)
                Assert.IsNull(corsOptions.Value.GetPolicy("DashboardCors"));
        }

        // Note: the "DashboardExtensions adds DashboardAuthorizationConvention when
        // AuthorizationPolicy is set" branch is not directly asserted here. Testing that
        // branch through IOptions<MvcOptions> or IConfigureOptions<MvcOptions> in a bare
        // ServiceCollection is unreliable — MVC's internal options pipeline does not
        // surface the Dashboard action's Conventions in a minimal DI container (the
        // filters are added but the conventions are dropped somewhere in the pipeline).
        // The branch guard is small (2 lines) and its observable behavior is tested via
        // DashboardAuthorizationConvention directly, below. End-to-end coverage belongs
        // in the Dashboard.Api.Integration.Tests project.

        [TestMethod]
        public void DashboardAuthorizationConvention_Apply_Adds_AuthorizeFilter_To_Dashboard_Controller()
        {
            var convention = new DashboardAuthorizationConvention("DashboardAdmin");
            var controllerType = typeof(ConnectionsController).GetTypeInfo();
            var controllerModel = new ControllerModel(controllerType, new List<object>());

            convention.Apply(controllerModel);

            Assert.AreEqual(1, (controllerModel.Filters.OfType<AuthorizeFilter>()).Count());
        }

        [TestMethod]
        public void DashboardAuthorizationConvention_Apply_Ignores_Controller_From_Other_Assembly()
        {
            var convention = new DashboardAuthorizationConvention("DashboardAdmin");
            // Use a type from a different assembly (System.Private.CoreLib) to exercise
            // the negative branch where the assembly-match check fails.
            var foreignControllerType = typeof(string).GetTypeInfo();
            var controllerModel = new ControllerModel(foreignControllerType, new List<object>());

            convention.Apply(controllerModel);

            Assert.IsFalse((controllerModel.Filters).Any());
        }
    }
}
