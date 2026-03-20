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
using DotNetWorkQueue.Dashboard.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DotNetWorkQueue.Dashboard.Api.Middleware
{
    /// <summary>
    /// When <see cref="DashboardOptions.ReadOnly"/> is true, blocks all non-GET requests
    /// with a 403 Forbidden response.
    /// </summary>
    internal class ReadOnlyFilter : IActionFilter
    {
        private readonly DashboardOptions _options;

        public ReadOnlyFilter(DashboardOptions options)
        {
            _options = options;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_options.ReadOnly) return;

            var method = context.HttpContext.Request.Method;
            if (method != "GET" && method != "HEAD" && method != "OPTIONS")
            {
                context.Result = new ObjectResult("Dashboard is in read-only mode. Write operations are disabled.")
                {
                    StatusCode = 403
                };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
