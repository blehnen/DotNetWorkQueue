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
using System.IO;
using System.Linq;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using DotNetWorkQueue.Dashboard.Api.Controllers;
using DotNetWorkQueue.Dashboard.Api.Middleware;
using DotNetWorkQueue.Dashboard.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Configuration;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.LiteDb.Basic;

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

            // Pre-load user assemblies so Newtonsoft's TypeNameHandling binder can resolve
            // message types during deserialization (before ResolveMessageBodyType runs).
            PreloadAssemblies(options.AssemblyPaths);

            services.AddSingleton(options);
            services.AddSingleton<IDashboardApi>(sp =>
            {
                var opts = sp.GetRequiredService<DashboardOptions>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
                    .CreateLogger<DashboardApi>();
                return new DashboardApi(opts, logger);
            });
            services.AddSingleton<IDashboardService, DashboardService>();
            services.AddSingleton<IConsumerRegistry, ConsumerRegistry>();

            if (options.EnableConsumerTracking)
            {
                services.AddHostedService<ConsumerPruningService>();
            }

            if (options.EnableCors && options.CorsOrigins.Length > 0)
            {
                services.AddCors(corsOptions =>
                {
                    corsOptions.AddPolicy("DashboardCors", policy =>
                    {
                        policy.WithOrigins(options.CorsOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
                });
            }

            services.AddHealthChecks();

            services.AddControllers(mvcOptions =>
                {
                    mvcOptions.Filters.Add<DashboardExceptionFilter>();
                    mvcOptions.Filters.Add(new ApiKeyAuthorizationFilter(options));
                    mvcOptions.Filters.Add(new ReadOnlyFilter(options));

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
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "DotNetWorkQueue Dashboard",
                        Version = "v1",
                        Description = "REST API for monitoring and managing DotNetWorkQueue"
                    });

                    if (!string.IsNullOrEmpty(options.ApiKey))
                    {
                        c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.ApiKey,
                            In = ParameterLocation.Header,
                            Name = "X-Api-Key",
                            Description = "API key for dashboard access"
                        });
                        c.AddSecurityRequirement(new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = "ApiKey"
                                    }
                                },
                                Array.Empty<string>()
                            }
                        });
                    }
                });
            }

            return services;
        }

        /// <summary>
        /// Adds DotNetWorkQueue Dashboard services configured from an IConfiguration section.
        /// Reads Dashboard:Connections[] entries and resolves transport types by name.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="dashboardSection">The "Dashboard" configuration section containing Connections, EnableSwagger, ApiKey, etc.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDotNetWorkQueueDashboard(
            this IServiceCollection services,
            Microsoft.Extensions.Configuration.IConfiguration dashboardSection)
        {
            var interceptorOptions = dashboardSection.GetSection("Interceptors")
                .Get<DashboardInterceptorOptions>();

            return services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = dashboardSection.GetValue("EnableSwagger", true);
                options.ApiKey = dashboardSection.GetValue<string>("ApiKey") ?? string.Empty;
                options.AssemblyPaths = dashboardSection.GetSection("AssemblyPaths").Get<string[]>() ?? Array.Empty<string>();

                foreach (var conn in dashboardSection.GetSection("Connections").GetChildren())
                {
                    var transport = conn["Transport"];
                    var connectionString = conn["ConnectionString"];
                    var displayName = conn["DisplayName"] ?? transport;
                    var queues = conn.GetSection("Queues").Get<string[]>() ?? Array.Empty<string>();

                    if (string.IsNullOrEmpty(transport))
                        throw new ArgumentException("Each Dashboard connection must specify a Transport.");
                    if (string.IsNullOrEmpty(connectionString))
                        throw new ArgumentException($"Dashboard connection '{displayName}' must specify a ConnectionString.");

                    AddConnectionByTransport(options, transport, connectionString, displayName!, queues, interceptorOptions);
                }
            });
        }

        /// <summary>
        /// Adds the DotNetWorkQueue Dashboard middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseDotNetWorkQueueDashboard(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<DashboardOptions>();

            if (options.EnableCors && options.CorsOrigins.Length > 0)
            {
                app.UseCors("DashboardCors");
            }

            if (options.EnableSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DotNetWorkQueue Dashboard v1");
                });
            }

            app.UseHealthChecks("/api/v1/dashboard/health");

            return app;
        }

        private static void AddConnectionByTransport(DashboardOptions options, string transport,
            string connectionString, string displayName, string[] queues,
            DashboardInterceptorOptions interceptors)
        {
            switch (transport)
            {
                case "SqlServer":
                    options.AddConnection<SqlServerMessageQueueInit>(connectionString, conn =>
                    {
                        conn.DisplayName = displayName;
                        foreach (var queue in queues)
                            conn.AddQueue(queue, interceptors);
                    });
                    break;
                case "PostgreSql":
                    options.AddConnection<PostgreSqlMessageQueueInit>(connectionString, conn =>
                    {
                        conn.DisplayName = displayName;
                        foreach (var queue in queues)
                            conn.AddQueue(queue, interceptors);
                    });
                    break;
                case "SQLite":
                    options.AddConnection<SqLiteMessageQueueInit>(connectionString, conn =>
                    {
                        conn.DisplayName = displayName;
                        foreach (var queue in queues)
                            conn.AddQueue(queue, interceptors);
                    });
                    break;
                case "LiteDb":
                    options.AddConnection<LiteDbMessageQueueInit>(connectionString, conn =>
                    {
                        conn.DisplayName = displayName;
                        foreach (var queue in queues)
                            conn.AddQueue(queue, interceptors);
                    });
                    break;
                case "Redis":
                    options.AddConnection<RedisQueueInit>(connectionString, conn =>
                    {
                        conn.DisplayName = displayName;
                        foreach (var queue in queues)
                            conn.AddQueue(queue, interceptors);
                    });
                    break;
                default:
                    throw new ArgumentException($"Unknown transport type: '{transport}'. Valid values: SqlServer, PostgreSql, SQLite, LiteDb, Redis.");
            }
        }

        private static void PreloadAssemblies(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return;

            foreach (var dir in paths)
            {
                if (!Directory.Exists(dir))
                    continue;

                foreach (var dll in Directory.GetFiles(dir, "*.dll"))
                {
                    try
                    {
                        System.Reflection.Assembly.LoadFrom(dll);
                    }
                    catch
                    {
                        // Not a valid .NET assembly — skip silently
                    }
                }
            }
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
