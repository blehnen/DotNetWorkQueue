using System.Runtime.InteropServices;
using System.Threading;

namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests
{
    /// <summary>
    /// Shared constants and helpers for Phase 3 integration tests.
    /// Port seeds are disjoint per test class to avoid TIME_WAIT collisions
    /// even though [assembly: DoNotParallelize] already serializes execution.
    /// </summary>
    internal static class TestHelpers
    {
        /// <summary>
        /// Beacon interface argument for <c>InjectDistributedTaskScheduler(port, beaconInterface)</c>.
        /// On Linux, the default "loopback" does NOT deliver UDP broadcasts back to sibling
        /// processes — use "" instead to bind to the first available interface.
        /// On Windows, "loopback" is correct.
        /// </summary>
        public static readonly string BeaconInterface =
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? string.Empty : "loopback";

        // Disjoint per-test-class port seeds (CONTEXT-3.md §5). Each test class owns
        // its own counter instance via a static field so ports never overlap.
        public const int EndToEndPortBase = 50000;
        public const int ConcurrencyPortBase = 55000;
        public const int NodeDiscoveryPortBase = 60000;

        /// <summary>
        /// Allocates the next TIME_WAIT-safe port from the given base. Thread-safe;
        /// callers hold a static counter per test class.
        /// </summary>
        public static int NextPort(ref int counter)
        {
            return Interlocked.Increment(ref counter);
        }
    }
}
