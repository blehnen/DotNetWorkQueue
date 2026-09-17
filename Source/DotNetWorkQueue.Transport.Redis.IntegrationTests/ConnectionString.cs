using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.Redis;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    /// <summary>
    /// Supplies the Redis endpoint the integration tests run against.
    /// </summary>
    /// <remarks>
    /// A connectionstring.txt in the output directory wins, which is how a developer points the
    /// suite at an instance they already have. With no such file the suite starts its own Redis
    /// container and owns it for the lifetime of the test process, so a run cannot see keys left
    /// behind by another run. That is the point of issue #281: these tests abuse Redis hard
    /// enough (4,000 messages across 25 workers, deliberately orphaned keys) that sharing one
    /// instance between concurrent runs produces failures belonging to neither of them.
    ///
    /// The Linq suite reaches this type through a ProjectReference rather than owning a copy, so
    /// both Redis assemblies resolve their endpoint here - but each is its own test process and
    /// therefore gets its own container.
    /// </remarks>
    public static class ConnectionInfo
    {
        /// <summary>
        /// Pinned to the version the shared CI instance has been running (redis_version 8.10.1),
        /// so moving to a per-run container changes the isolation and nothing else. Pinning is
        /// deliberate for the same reason the Playwright browser version is pinned in the CI
        /// image: a silently-moving server version turns an unrelated build red.
        /// </summary>
        private const string RedisImage = "redis:8.10.1-alpine";

        /// <summary>
        /// The client options the shared instance was always reached with. StackExchange.Redis
        /// defaults both timeouts to 5000ms, which is under what the synchronous EVAL paths need
        /// when the suite injects in bursts - see AssemblyInit and issue #161. Appending them
        /// keeps a container run and a connectionstring.txt run talking to Redis the same way.
        /// </summary>
        private const string ClientOptions = ",defaultDatabase=1,connectTimeout=15000,syncTimeout=7000";

        private const string ConnectionStringFile = "connectionstring.txt";

        /// <summary>
        /// Set by CI for the stages that are supposed to own their service. A connectionstring.txt
        /// left in the output directory by an earlier build takes precedence over starting a
        /// container, and agents keep their workspace between builds - so without this a run could
        /// quietly go back to a shared server and still report every test as passing. Green has to
        /// mean the container was used, not merely that the tests were happy.
        /// </summary>
        private static bool RequireOwnContainer =>
            string.Equals(Environment.GetEnvironmentVariable("DNWQ_REQUIRE_SERVICE_CONTAINERS"),
                "true", StringComparison.OrdinalIgnoreCase);

        private static readonly object Sync = new object();
        private static string _connectionString;
        private static RedisContainer _container;

        /// <summary>
        /// The Redis endpoint, starting the container on first use if no connectionstring.txt exists.
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                var resolved = Volatile.Read(ref _connectionString);
                if (!string.IsNullOrEmpty(resolved))
                    return resolved;

                lock (Sync)
                {
                    if (string.IsNullOrEmpty(_connectionString))
                        _connectionString = Resolve();

                    return _connectionString;
                }
            }
        }

        /// <summary>
        /// Resolves the endpoint up front so the container start is paid during assembly
        /// initialization rather than inside whichever test happens to touch Redis first. That
        /// test would otherwise absorb the image pull against its own timeouts.
        /// </summary>
        public static void EnsureStarted()
        {
            _ = ConnectionString;

            if (!RequireOwnContainer)
                return;

            bool startedOwnContainer;
            lock (Sync)
            {
                startedOwnContainer = _container != null;
            }

            if (!startedOwnContainer)
                throw new InvalidOperationException(
                    "DNWQ_REQUIRE_SERVICE_CONTAINERS is set, so this suite must start its own " +
                    "Redis, but the endpoint came from a connectionstring.txt in the output " +
                    "directory instead. That file would send the run against a shared server while " +
                    "every test still passed. See issue #281.");
        }

        /// <summary>
        /// Disposes the container if this process started one. Testcontainers' reaper would remove
        /// it when the process exits regardless; doing it explicitly returns the memory before the
        /// next assembly in the same agent starts its own.
        /// </summary>
        public static void Shutdown()
        {
            RedisContainer container;
            lock (Sync)
            {
                container = _container;
                _container = null;
                _connectionString = null;
            }

            container?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        private static string Resolve()
        {
            var fromFile = ReadConnectionStringFile();
            if (!string.IsNullOrEmpty(fromFile))
                return fromFile;

            var container = new RedisBuilder(RedisImage).Build();

            // Task.Run keeps the blocking wait off any context the caller captured; assembly
            // initialization runs before the tests clear theirs.
            Task.Run(() => container.StartAsync()).GetAwaiter().GetResult();
            _container = container;

            return container.GetConnectionString() + ClientOptions;
        }

        private static string ReadConnectionStringFile()
        {
            if (!File.Exists(ConnectionStringFile))
                return null;

            return File.ReadAllText(ConnectionStringFile).Trim();
        }
    }
}
