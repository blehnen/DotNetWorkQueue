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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Dashboard.Api.Middleware
{
    /// <summary>
    /// Exception filter for dashboard API controllers. Maps known exceptions to appropriate HTTP status codes.
    /// </summary>
    internal class DashboardExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<DashboardExceptionFilter> _logger;

        public DashboardExceptionFilter(ILogger<DashboardExceptionFilter> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public void OnException(ExceptionContext context)
        {
            switch (context.Exception)
            {
                case ObjectDisposedException:
                    _logger.LogWarning(context.Exception, "Dashboard service was disposed while handling {Method} {Path}",
                        context.HttpContext.Request.Method, context.HttpContext.Request.Path);
                    context.Result = new ObjectResult(new { error = "Service unavailable" })
                    {
                        StatusCode = 503
                    };
                    context.ExceptionHandled = true;
                    break;

                case InvalidOperationException:
                    _logger.LogWarning(context.Exception, "Resource not found: {Message}", context.Exception.Message);
                    context.Result = new ObjectResult(new { error = context.Exception.Message })
                    {
                        StatusCode = 404
                    };
                    context.ExceptionHandled = true;
                    break;

                case NotSupportedException:
                    _logger.LogWarning(context.Exception, "Unsupported operation: {Message}", context.Exception.Message);
                    context.Result = new ObjectResult(new { error = context.Exception.Message })
                    {
                        StatusCode = 501
                    };
                    context.ExceptionHandled = true;
                    break;
            }
        }
    }
}
