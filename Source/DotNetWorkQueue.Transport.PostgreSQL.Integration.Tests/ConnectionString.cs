using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests
{
    /// <summary>
    /// Supplies the PostgreSQL endpoint the integration tests run against.
    /// </summary>
    /// <remarks>
    /// A connectionstring.txt in the output directory wins, which is how a developer points the
    /// suite at a server they already have. With no such file the suite starts its own PostgreSQL
    /// container and owns it for the lifetime of the test process, so a run cannot inherit tables
    /// a previous run failed to drop - see issue #281.
    ///
    /// The Linq suite reaches this type through a ProjectReference rather than owning a copy, so
    /// both PostgreSQL assemblies resolve their endpoint here - but each is its own test process
    /// and therefore gets its own container.
    /// </remarks>
    public static class ConnectionInfo
    {
        /// <summary>
        /// Mirrors the image the shared server runs (postgres:14). Kept in step deliberately: the
        /// point of owning the instance is to remove the interference, not to silently move to a
        /// different major version at the same time.
        /// </summary>
        private const string PostgreSqlImage = "postgres:14";

        private const string Database = "integrationtesting";
        private const string Username = "postgres";
        private const string Password = "integrationtesting";

        /// <summary>
        /// The shared server is configured with max_connections=500; a stock postgres image
        /// allows 100. The consumer suites open a connection per worker and the pool ceiling
        /// below is 250, so the container has to be raised to match or the suite can exhaust
        /// the server rather than its own pool.
        /// </summary>
        private const string MaxConnections = "500";

        /// <summary>
        /// The options the shared server was always reached with. The pool ceiling matters most:
        /// the consumer suites open a connection per worker and Npgsql defaults Maximum Pool Size
        /// to 100, which is below what they ask for.
        /// </summary>
        private const string ClientOptions = ";Maximum Pool Size=250;Trust Server Certificate=true;Keepalive=15;Tcp Keepalive=true;";

        private const string ConnectionStringFile = "connectionstring.txt";

        private static readonly object Sync = new object();
        private static string _connectionString;
        private static PostgreSqlContainer _container;

        /// <summary>
        /// The PostgreSQL endpoint, starting the container on first use if no connectionstring.txt exists.
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
        /// Resolves the endpoint up front so a container start is paid during assembly
        /// initialization rather than inside whichever test happens to touch the database first.
        /// </summary>
        public static void EnsureStarted()
        {
            _ = ConnectionString;
        }

        /// <summary>
        /// Disposes the container if this process started one. Testcontainers' reaper would remove
        /// it when the process exits regardless; doing it explicitly returns the memory before the
        /// next assembly on the same agent starts its own.
        /// </summary>
        public static void Shutdown()
        {
            PostgreSqlContainer container;
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

            var container = new PostgreSqlBuilder(PostgreSqlImage)
                .WithDatabase(Database)
                .WithUsername(Username)
                .WithPassword(Password)
                .WithCommand("-c", "max_connections=" + MaxConnections)
                .Build();

            // Task.Run keeps the blocking wait off any context the caller captured.
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
