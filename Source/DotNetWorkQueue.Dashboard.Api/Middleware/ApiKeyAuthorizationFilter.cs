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
using DotNetWorkQueue.Dashboard.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DotNetWorkQueue.Dashboard.Api.Middleware
{
    /// <summary>
    /// Action filter that validates the <c>X-Api-Key</c> header against <see cref="DashboardOptions.ApiKey"/>.
    /// Only active when <see cref="DashboardOptions.ApiKey"/> is configured.
    /// </summary>
    internal class ApiKeyAuthorizationFilter : IAuthorizationFilter
    {
        private const string HeaderName = "X-Api-Key";
        private readonly DashboardOptions _options;

        public ApiKeyAuthorizationFilter(DashboardOptions options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (string.IsNullOrEmpty(_options.ApiKey))
                return;

            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
                || !string.Equals(providedKey, _options.ApiKey, StringComparison.Ordinal))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing API key." });
            }
        }
    }
}
