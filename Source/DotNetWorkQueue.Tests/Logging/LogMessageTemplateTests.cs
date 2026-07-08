using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Logging
{
    /// <summary>
    /// Verifies that the structured-logging message templates introduced for CA2254/S2629
    /// render to the same text the original string interpolation produced. Guards against a
    /// placeholder/argument mismatch, which the logging framework would render silently
    /// (no exception) rather than fail.
    /// </summary>
    [TestClass]
    public class LogMessageTemplateTests
    {
        [TestMethod]
        public void SinglePlaceholder_RendersInterpolatedEquivalent()
        {
            var logger = new CapturingLogger();
            const string id = "msg-1";
            logger.LogDebug("HeartBeat processing completed {MessageId}", id);
            Assert.AreEqual($"HeartBeat processing completed {id}", logger.LastMessage);
        }

        [TestMethod]
        public void TwoPlaceholders_RendersInterpolatedEquivalent()
        {
            var logger = new CapturingLogger();
            const int count = 5;
            const string queue = "q1";
            logger.LogInformation("Deleted {Count} error messages from {QueueName}", count, queue);
            Assert.AreEqual($"Deleted {count} error messages from {queue}", logger.LastMessage);
        }

        [TestMethod]
        public void RepeatedNewLinePlaceholders_PreserveMultiLineOutput()
        {
            var logger = new CapturingLogger();
            var nl = Environment.NewLine;
            const string id = "id-1";
            const string err = "boom";
            logger.LogWarning(
                "Heart beat processing has triggered a rollback; rollbacks are not supported for heartbeats {NewLine}{MessageId}{NewLine2}{Error}",
                nl, id, nl, err);
            Assert.AreEqual(
                $"Heart beat processing has triggered a rollback; rollbacks are not supported for heartbeats {nl}{id}{nl}{err}",
                logger.LastMessage);
        }

        [TestMethod]
        public void ExceptionArgument_RendersToStringInline()
        {
            var logger = new CapturingLogger();
            var nl = Environment.NewLine;
            var ex = new InvalidOperationException("kaboom");
            logger.LogError("An error has occurred while trying to rollback a message{NewLine}{Exception}", nl, ex);
            Assert.AreEqual($"An error has occurred while trying to rollback a message{nl}{ex}", logger.LastMessage);
        }

        [TestMethod]
        public void RetryTemplate_RendersInterpolatedEquivalent()
        {
            var logger = new CapturingLogger();
            var nl = Environment.NewLine;
            const double retryMs = 250d;
            const int attempt = 3;
            var ex = new TimeoutException("db");
            logger.LogWarning(
                "An error has occurred; we will try to re-run the transaction in {RetryDelayMs} ms. An error has occurred {AttemptNumber} times{NewLine}{Exception}",
                retryMs, attempt, nl, ex);
            Assert.AreEqual(
                $"An error has occurred; we will try to re-run the transaction in {retryMs} ms. An error has occurred {attempt} times{nl}{ex}",
                logger.LastMessage);
        }

        private sealed class CapturingLogger : ILogger
        {
            public string LastMessage { get; private set; }
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                LastMessage = formatter(state, exception);
            }
        }
    }
}
