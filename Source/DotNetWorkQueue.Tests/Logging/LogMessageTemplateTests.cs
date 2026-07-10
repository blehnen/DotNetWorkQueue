using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Logging
{
    /// <summary>
    /// Verifies the structured-logging message templates: that placeholders render to the
    /// expected text (guarding the CA2254/S2629 templating, which the framework renders
    /// silently on a placeholder/argument mismatch rather than failing), and that exceptions
    /// flow to the dedicated exception parameter rather than an inline {Exception} placeholder
    /// (S6668).
    /// </summary>
    [TestClass]
    public class LogMessageTemplateTests
    {
        [TestMethod]
        public void SinglePlaceholder_RendersInterpolatedEquivalent()
        {
            var logger = new CapturingLogger();
            const string id = "msg-1";
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("HeartBeat processing completed {MessageId}", id);
            Assert.AreEqual($"HeartBeat processing completed {id}", logger.LastMessage);
        }

        [TestMethod]
        public void TwoPlaceholders_RendersInterpolatedEquivalent()
        {
            var logger = new CapturingLogger();
            const int count = 5;
            const string queue = "q1";
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Deleted {Count} error messages from {QueueName}", count, queue);
            Assert.AreEqual($"Deleted {count} error messages from {queue}", logger.LastMessage);
        }

        [TestMethod]
        public void HeartbeatRollback_ExceptionToParameter_MessageIdPlaceholder()
        {
            var logger = new CapturingLogger();
            const string id = "id-1";
            var ex = new InvalidOperationException("boom");
            logger.LogWarning(ex,
                "Heart beat processing has triggered a rollback; rollbacks are not supported for heartbeats {MessageId}",
                id);
            Assert.AreEqual(
                $"Heart beat processing has triggered a rollback; rollbacks are not supported for heartbeats {id}",
                logger.LastMessage);
            Assert.AreSame(ex, logger.LastException);
        }

        [TestMethod]
        public void ExceptionArgument_PassedToExceptionParameter()
        {
            var logger = new CapturingLogger();
            var ex = new InvalidOperationException("kaboom");
            logger.LogError(ex, "An error has occurred while trying to rollback a message");
            Assert.AreEqual("An error has occurred while trying to rollback a message", logger.LastMessage);
            Assert.AreSame(ex, logger.LastException);
        }

        [TestMethod]
        public void RetryTemplate_ExceptionToParameter_RendersPlaceholders()
        {
            var logger = new CapturingLogger();
            const double retryMs = 250d;
            const int attempt = 3;
            var ex = new TimeoutException("db");
            logger.LogWarning(ex,
                "An error has occurred; we will try to re-run the transaction in {RetryDelayMs} ms. An error has occurred {AttemptNumber} times",
                retryMs, attempt);
            Assert.AreEqual(
                $"An error has occurred; we will try to re-run the transaction in {retryMs} ms. An error has occurred {attempt} times",
                logger.LastMessage);
            Assert.AreSame(ex, logger.LastException);
        }

        private sealed class CapturingLogger : ILogger
        {
            public string LastMessage { get; private set; }
            public Exception LastException { get; private set; }
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                LastMessage = formatter(state, exception);
                LastException = exception;
            }
        }
    }
}
