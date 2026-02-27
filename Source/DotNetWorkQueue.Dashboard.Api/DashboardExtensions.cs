// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Linq;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using DotNetWorkQueue.Dashboard.Api.Controllers;
using DotNetWorkQueue.Dashboard.Api.Middleware;
using DotNetWorkQueue.Dashboard.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetWorkQueue.Dashboard.Api
{
    /// <summary>
    /// Extension methods for integrating the DotNetWorkQueue Dashboard into an ASP.NET Core application.
    /// </summary>
    public static class DashboardExtensions
    {
        /// <summary>
        /// Adds DotNetWorkQueue Dashboard services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure dashboard options including connections and queues.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDotNetWorkQueueDashboard(
            this IServiceCollection services,
            Action<DashboardOptions> configureOptions)
        {
            var options = new DashboardOptions();
            configureOptions(options);

            services.AddSingleton(options);
            services.AddSingleton<IDashboardApi>(sp =>
            {
                var opts = sp.GetRequiredService<DashboardOptions>();
                return new DashboardApi(opts);
            });
            services.AddSingleton<IDashboardService, DashboardService>();

            services.AddControllers(mvcOptions =>
                {
                    mvcOptions.Filters.Add<DashboardExceptionFilter>();

                    if (!string.IsNullOrEmpty(options.AuthorizationPolicy))
                    {
                        mvcOptions.Conventions.Add(
                            new DashboardAuthorizationConvention(options.AuthorizationPolicy));
                    }
                })
                .AddApplicationPart(typeof(DashboardExtensions).Assembly);

            if (options.EnableSwagger)
            {
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                    {
                        Title = "DotNetWorkQueue Dashboard",
                        Version = "v1",
                        Description = "REST API for monitoring and managing DotNetWorkQueue"
                    });
                });
            }

            return services;
        }

        /// <summary>
        /// Adds the DotNetWorkQueue Dashboard middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseDotNetWorkQueueDashboard(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<DashboardOptions>();

            if (options.EnableSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DotNetWorkQueue Dashboard v1");
                });
            }

            return app;
        }
    }

    /// <summary>
    /// MVC convention that applies an authorization policy to dashboard controllers.
    /// </summary>
    internal class DashboardAuthorizationConvention : IControllerModelConvention
    {
        private readonly string _policy;

        public DashboardAuthorizationConvention(string policy)
        {
            _policy = policy;
        }

        public void Apply(ControllerModel controller)
        {
            var dashboardAssembly = typeof(ConnectionsController).Assembly;
            if (controller.ControllerType.Assembly == dashboardAssembly)
            {
                controller.Filters.Add(new AuthorizeFilter(_policy));
            }
        }
    }
}
