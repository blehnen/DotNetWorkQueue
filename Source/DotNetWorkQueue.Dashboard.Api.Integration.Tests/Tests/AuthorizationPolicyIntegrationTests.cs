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
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class AuthorizationPolicyIntegrationTests
    {
        private DashboardTestServer _server;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _server = await DashboardTestServer.CreateAsync(
                options =>
                {
                    options.EnableSwagger = false;
                    options.AuthorizationPolicy = "DashboardAdmin";
                },
                services =>
                {
                    // Register a minimal auth scheme and a policy that requires an authenticated
                    // user. When an unauthenticated request hits a dashboard controller, the
                    // AuthorizeFilter added by DashboardAuthorizationConvention will challenge.
                    services.AddAuthentication(options => options.DefaultScheme = "Test")
                        .AddScheme<AuthenticationSchemeOptions, NoAuthHandler>("Test", _ => { });
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy("DashboardAdmin",
                            policy => policy.RequireAuthenticatedUser());
                    });
                },
                app =>
                {
                    app.UseAuthentication();
                    app.UseAuthorization();
                });
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server != null) await _server.DisposeAsync();
        }

        [TestMethod]
        public async Task DashboardController_Unauthenticated_Returns_Unauthorized_When_AuthorizationPolicy_Set()
        {
            // The DashboardAuthorizationConvention should have applied the AuthorizeFilter
            // to dashboard-assembly controllers. Hit an MVC controller (not /health which is
            // middleware, not a controller) and assert an unauthenticated 401 response.
            var response = await _server.Client.GetAsync("api/v1/dashboard/connections");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private class NoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public NoAuthHandler(
                IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger,
                UrlEncoder encoder)
                : base(options, logger, encoder)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
        }
    }
}
