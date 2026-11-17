using System;
using System.Collections.Concurrent;
using System.Linq;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Extensions
{
    [TestClass]
    public class DashboardExtensionsUnprotectedWarningTests
    {
        [TestMethod]
        public void WarnIfUnprotected_NoApiKey_NoPolicy_NotReadOnly_LogsOneWarning()
        {
            var logger = new CapturingLogger();

            DashboardExtensions.WarnIfUnprotected(new DashboardOptions(), logger);

            Assert.HasCount(1, logger.Entries);
            Assert.AreEqual(LogLevel.Warning, logger.Entries.Single().Level);
            Assert.Contains("unprotected", logger.Entries.Single().Message);
        }

        [TestMethod]
        public void WarnIfUnprotected_ApiKeySet_LogsNothing()
        {
            var logger = new CapturingLogger();

            DashboardExtensions.WarnIfUnprotected(new DashboardOptions { ApiKey = "secret" }, logger);

            Assert.IsEmpty(logger.Entries);
        }

        [TestMethod]
        public void WarnIfUnprotected_AuthorizationPolicySet_LogsNothing()
        {
            var logger = new CapturingLogger();

            DashboardExtensions.WarnIfUnprotected(new DashboardOptions { AuthorizationPolicy = "DashboardAdmins" }, logger);

            Assert.IsEmpty(logger.Entries);
        }

        [TestMethod]
        public void WarnIfUnprotected_ReadOnly_LogsNothing()
        {
            var logger = new CapturingLogger();

            DashboardExtensions.WarnIfUnprotected(new DashboardOptions { ReadOnly = true }, logger);

            Assert.IsEmpty(logger.Entries);
        }

        [TestMethod]
        public void UseDotNetWorkQueueDashboard_Unprotected_LogsTheWarning()
        {
            var logger = UseDashboard(_ => { });

            Assert.HasCount(1, logger.Entries.Where(e => e.Level == LogLevel.Warning && e.Message.Contains("unprotected")));
        }

        [TestMethod]
        public void UseDotNetWorkQueueDashboard_WithApiKey_DoesNotLogTheWarning()
        {
            var logger = UseDashboard(o => o.ApiKey = "secret");

            Assert.IsEmpty(logger.Entries.Where(e => e.Message.Contains("unprotected")));
        }

        private static CapturingLogger UseDashboard(Action<DashboardOptions> configure)
        {
            var logger = new CapturingLogger();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(new CapturingLoggerProvider(logger)));
            services.AddDotNetWorkQueueDashboard(o =>
            {
                o.EnableSwagger = false;
                configure(o);
            });

            using var provider = services.BuildServiceProvider();
            new ApplicationBuilder(provider).UseDotNetWorkQueueDashboard();
            return logger;
        }

        private sealed class CapturingLoggerProvider : ILoggerProvider
        {
            private readonly CapturingLogger _logger;
            public CapturingLoggerProvider(CapturingLogger logger) => _logger = logger;
            public ILogger CreateLogger(string categoryName) => _logger;
            public void Dispose() { }
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

            public (LogLevel Level, string Message)[] Entries => _entries.ToArray();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                _entries.Enqueue((logLevel, formatter(state, exception)));
            }
        }
    }
}
