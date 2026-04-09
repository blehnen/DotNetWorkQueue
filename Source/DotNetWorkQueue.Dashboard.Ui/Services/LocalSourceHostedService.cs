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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    /// <summary>
    /// Hosted service that resolves the actual listen address of the in-process API
    /// after the server starts, and updates the "Local" source in the <see cref="ISourceRegistry"/>.
    /// </summary>
    public class LocalSourceHostedService : IHostedService
    {
        private readonly IServer _server;
        private readonly ISourceRegistry _sourceRegistry;
        private readonly ILogger<LocalSourceHostedService> _logger;

        /// <summary>
        /// Creates a new <see cref="LocalSourceHostedService"/>.
        /// </summary>
        /// <param name="server">The ASP.NET Core server instance.</param>
        /// <param name="sourceRegistry">The registry of configured API sources.</param>
        /// <param name="logger">The logger.</param>
        public LocalSourceHostedService(IServer server, ISourceRegistry sourceRegistry, ILogger<LocalSourceHostedService> logger)
        {
            _server = server;
            _sourceRegistry = sourceRegistry;
            _logger = logger;
        }

        /// <summary>
        /// Resolves the server's actual listen address and updates the Local source's BaseUrl.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A completed task.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var addressesFeature = _server.Features.Get<IServerAddressesFeature>();
            if (addressesFeature == null || !addressesFeature.Addresses.Any())
            {
                _logger.LogWarning("Could not resolve local server address: IServerAddressesFeature is unavailable or has no addresses. The Local source will use its configured placeholder URL.");
                return Task.CompletedTask;
            }

            var address = addressesFeature.Addresses.First();
            var localSource = _sourceRegistry.GetByName("Local");
            if (localSource != null)
            {
                localSource.BaseUrl = address;
                _logger.LogInformation("Local API source URL resolved to {Address}", address);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// No-op stop.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A completed task.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
