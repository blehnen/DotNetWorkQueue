using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    /// <summary>
    /// Checks for the error tests, whose handler always throws: every message must be attempted
    /// exactly <c>attemptsPerMessage</c> times.
    /// </summary>
    /// <remarks>
    /// Without these, a run that stalled or skipped its retries waited out its timeout and carried
    /// on as though it had finished (GitHub #428).
    /// </remarks>
    public static class ErrorAttempts
    {
        /// <summary>
        /// Waits for the last expected attempt, and fails if it does not arrive in time.
        /// </summary>
        public static void AssertReached(ManualResetEventSlim waitForFinish, int timeOutSeconds,
            IncrementWrapper processedCount, int messageCount, int attemptsPerMessage)
        {
            Assert.IsTrue(waitForFinish.Wait(timeOutSeconds * 1000),
                $"Expected {messageCount * attemptsPerMessage} attempts ({attemptsPerMessage} per message) " +
                $"within {timeOutSeconds} seconds, but saw {Interlocked.Read(ref processedCount.ProcessedCount)}");
        }

        /// <summary>
        /// Fails if a message was attempted more often than expected.
        /// </summary>
        public static void AssertNoMore(IncrementWrapper processedCount, int messageCount, int attemptsPerMessage)
        {
            Assert.AreEqual(messageCount * attemptsPerMessage, Interlocked.Read(ref processedCount.ProcessedCount),
                $"Expected {attemptsPerMessage} attempts per message");
        }
    }
}
