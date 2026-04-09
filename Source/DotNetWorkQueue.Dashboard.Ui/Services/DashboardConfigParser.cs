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
using Microsoft.Extensions.Configuration;

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    /// <summary>
    /// Parses and validates Dashboard API source configuration
    /// from <see cref="global::Microsoft.Extensions.Configuration.IConfiguration"/>.
    /// Detects legacy single-source format and provides migration instructions.
    /// </summary>
    public static class DashboardConfigParser
    {
        /// <summary>
        /// Parses the <c>DashboardApi:Sources</c> configuration section into a list of
        /// <see cref="DashboardApiSourceConfig"/> instances.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>A list of configured API sources. May be empty if no sources are configured.</returns>
        public static List<DashboardApiSourceConfig> ParseSources(global::Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var sources = new List<DashboardApiSourceConfig>();
            configuration.GetSection("DashboardApi:Sources").Bind(sources);
            return sources;
        }

        /// <summary>
        /// Validates that the configuration does not contain the legacy single-source format
        /// (<c>DashboardApi:BaseUrl</c> / <c>DashboardApi:ApiKey</c>) without a <c>DashboardApi:Sources</c> section.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the legacy flat format is detected without a <c>DashboardApi:Sources</c> section.
        /// </exception>
        public static void ValidateNoLegacyConfig(global::Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var hasLegacyBaseUrl = !string.IsNullOrEmpty(configuration["DashboardApi:BaseUrl"]);
            var hasSourcesSection = configuration.GetSection("DashboardApi:Sources").GetChildren().Any();

            if (hasLegacyBaseUrl && !hasSourcesSection)
            {
                throw new InvalidOperationException(
                    """
                    Legacy single-source configuration detected. The flat 'DashboardApi:BaseUrl' / 'DashboardApi:ApiKey' format is no longer supported.

                    Migrate to the new multi-source format in appsettings.json:

                    "DashboardApi": {
                      "Sources": [
                        {
                          "Name": "Local",
                          "BaseUrl": "http://localhost:5000",
                          "ApiKey": "your-key-here"
                        }
                      ]
                    }

                    Remove the 'DashboardApi:BaseUrl' and 'DashboardApi:ApiKey' keys after migration.
                    """);
            }
        }
    }
}
