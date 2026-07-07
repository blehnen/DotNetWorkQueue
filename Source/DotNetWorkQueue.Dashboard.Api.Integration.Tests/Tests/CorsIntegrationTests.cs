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
using System.Net.Http;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class CorsIntegrationTests
    {
        private static readonly string[] CorsOriginsValue = { "https://example.com" };
        private DashboardTestServer _server;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.EnableCors = true;
                options.CorsOrigins = CorsOriginsValue;
            });
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server != null) await _server.DisposeAsync();
        }

        [TestMethod]
        public async Task CorsRequest_ReturnsAllowOriginHeader_WhenOriginMatches()
        {
            // Send a GET with an Origin header (not a preflight OPTIONS — simpler and
            // sufficient to exercise the UseCors("DashboardCors") branch in the pipeline).
            var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/dashboard/health");
            request.Headers.Add("Origin", "https://example.com");

            var response = await _server.Client.SendAsync(request);

            Assert.IsTrue(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins),
                "because the CORS middleware should have added the allow-origin header for the matching origin");
            Assert.Contains("https://example.com", origins);
        }
    }
}
