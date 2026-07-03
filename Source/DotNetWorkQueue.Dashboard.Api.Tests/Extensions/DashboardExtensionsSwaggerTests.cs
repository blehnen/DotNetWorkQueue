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
using DotNetWorkQueue.Dashboard.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Extensions
{
    [TestClass]
    public class DashboardExtensionsSwaggerTests
    {
        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Registers_SwaggerServices_When_Enabled()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = true;
            });

            var provider = services.BuildServiceProvider();
            var swaggerOptions = provider
                .GetRequiredService<IOptions<SwaggerGenOptions>>()
                .Value;

            Assert.IsTrue((swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs).ContainsKey("v1"));
            Assert.AreEqual("DotNetWorkQueue Dashboard", swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs["v1"].Title);
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Registers_ApiKeySecurityScheme_When_ApiKey_Set()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = true;
                options.ApiKey = "test-secret";
            });

            var provider = services.BuildServiceProvider();
            var swaggerOptions = provider
                .GetRequiredService<IOptions<SwaggerGenOptions>>()
                .Value;

            Assert.IsTrue((swaggerOptions.SwaggerGeneratorOptions.SecuritySchemes).ContainsKey("ApiKey"));
            var scheme = swaggerOptions.SwaggerGeneratorOptions.SecuritySchemes["ApiKey"];
            Assert.AreEqual(SecuritySchemeType.ApiKey, scheme.Type);
            Assert.AreEqual(ParameterLocation.Header, scheme.In);
            Assert.AreEqual("X-Api-Key", scheme.Name);
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Does_Not_Register_ApiKeySecurityScheme_When_ApiKey_Empty()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = true;
                // ApiKey deliberately left at default (empty string)
            });

            var provider = services.BuildServiceProvider();
            var swaggerOptions = provider
                .GetRequiredService<IOptions<SwaggerGenOptions>>()
                .Value;

            Assert.IsFalse((swaggerOptions.SwaggerGeneratorOptions.SecuritySchemes).ContainsKey("ApiKey"));
        }
    }
}
