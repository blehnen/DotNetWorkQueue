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
using System.Net.Http;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers
{
    public class DashboardTestServer : IAsyncDisposable
    {
        private readonly WebApplication _app;

        public HttpClient Client { get; }

        private DashboardTestServer(WebApplication app, HttpClient client)
        {
            _app = app;
            Client = client;
        }

        public static async Task<DashboardTestServer> CreateAsync(Action<DashboardOptions> configure)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddDotNetWorkQueueDashboard(configure);

            var app = builder.Build();
            app.UseDotNetWorkQueueDashboard();
            app.MapControllers();

            await app.StartAsync();
            var client = app.GetTestClient();
            return new DashboardTestServer(app, client);
        }

        public async ValueTask DisposeAsync()
        {
            Client?.Dispose();
            if (_app != null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
        }
    }
}
