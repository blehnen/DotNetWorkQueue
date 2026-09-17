using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests
{
    /// <summary>
    /// Supplies the SQL Server endpoint the integration tests run against.
    /// </summary>
    /// <remarks>
    /// A connectionstring.txt in the output directory wins, which is how a developer points the
    /// suite at a server they already have. With no such file the suite starts its own SQL Server
    /// container and owns it for the lifetime of the test process - see issue #281.
    ///
    /// Unlike Redis and PostgreSQL, the container is not usable as it comes: the suite needs a
    /// named database and two non-default schemas, so both are created after the container starts.
    /// </remarks>
    public static class ConnectionInfo
    {
        /// <summary>
        /// Matches the image the shared server runs. Also why the transport uses CASE WHEN rather
        /// than a two-argument GREATEST, which is 2022 and later.
        /// </summary>
        private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2019-latest";

        private const string Database = "IntegrationTests";
        private const string Password = "IntegrationTests!Pass1";
        private const string ConnectionStringFile = "connectionstring.txt";

        /// <summary>
        /// Set by CI for the stages that are supposed to own their service. A connectionstring.txt
        /// left in the output directory by an earlier build takes precedence over starting a
        /// container, and agents keep their workspace between builds - so without this a run could
        /// quietly go back to a shared server and still report every test as passing.
        /// </summary>
        private static bool RequireOwnContainer =>
            string.Equals(Environment.GetEnvironmentVariable("DNWQ_REQUIRE_SERVICE_CONTAINERS"),
                "true", StringComparison.OrdinalIgnoreCase);

        private static readonly object Sync = new object();
        private static string _connectionString;
        private static MsSqlContainer _container;

        /// <summary>
        /// The SQL Server endpoint, starting the container on first use if no connectionstring.txt exists.
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

        /// <summary>The default schema. Queues live here unless a test says otherwise.</summary>
        public static string SchemaDefault = "dbo";

        /// <summary>
        /// Non-default schemas, proving a queue does not have to live in dbo. They are created by
        /// <see cref="EnsureSchemas"/> rather than by hand: they were hand-made on the shared
        /// server once and had since been lost, which went unnoticed because the only test using
        /// them never awaited its own work. See issue #281.
        /// </summary>
        public static string Schema1 = "test1";

        /// <inheritdoc cref="Schema1"/>
        public static string Schema2 = "test2";

        /// <summary>
        /// Resolves the endpoint up front so a container start is paid during assembly
        /// initialization rather than inside whichever test happens to touch the database first,
        /// and fails loudly if CI required a container and did not get one.
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
                    "DNWQ_REQUIRE_SERVICE_CONTAINERS is set, so this suite must start its own SQL " +
                    "Server, but the endpoint came from a connectionstring.txt in the output " +
                    "directory instead. That file would send the run against a shared server while " +
                    "every test still passed. See issue #281.");
        }

        /// <summary>
        /// Disposes the container if this process started one. A SQL Server container holds a lot
        /// more memory than the other two, so returning it promptly matters on a shared agent.
        /// </summary>
        public static void Shutdown()
        {
            MsSqlContainer container;
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
            {
                // A server someone already had will not necessarily have the non-default schemas,
                // and creating them is additive and idempotent.
                EnsureSchemas(fromFile);
                return fromFile;
            }

            var container = new MsSqlBuilder(SqlServerImage)
                .WithPassword(Password)
                .Build();

            Task.Run(() => container.StartAsync()).GetAwaiter().GetResult();
            _container = container;

            var connectionString = BuildConnectionString(container);
            CreateDatabase(container);
            EnsureSchemas(connectionString);
            return connectionString;
        }

        /// <summary>
        /// The container comes up with master only. The suite's connection string names its own
        /// database, so create it before anything tries to connect to it.
        /// </summary>
        private static void CreateDatabase(MsSqlContainer container)
        {
            var result = Task.Run(() => container.ExecScriptAsync(
                    $"IF DB_ID('{Database}') IS NULL CREATE DATABASE [{Database}];"))
                .GetAwaiter().GetResult();

            if (result.ExitCode != 0)
                throw new InvalidOperationException(
                    $"Could not create the {Database} database in the SQL Server container: {result.Stderr}");
        }

        /// <summary>
        /// Creates the non-default schemas if they are missing. CREATE SCHEMA has to be the only
        /// statement in its batch, hence EXEC.
        /// </summary>
        private static void EnsureSchemas(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach (var schema in new[] { Schema1, Schema2 })
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            $"IF SCHEMA_ID('{schema}') IS NULL EXEC('CREATE SCHEMA [{schema}]');";
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// The container hands back a connection string for master with no pooling or application
        /// settings. Carry over what the shared server was always reached with, so a container run
        /// and a connectionstring.txt run behave the same.
        /// </summary>
        private static string BuildConnectionString(MsSqlContainer container)
        {
            return new SqlConnectionStringBuilder(container.GetConnectionString())
            {
                InitialCatalog = Database,
                ApplicationName = "IntegrationTesting",
                MaxPoolSize = 500,
                TrustServerCertificate = true
            }.ConnectionString;
        }

        private static string ReadConnectionStringFile()
        {
            if (!File.Exists(ConnectionStringFile))
                return null;

            return File.ReadAllText(ConnectionStringFile).Trim();
        }
    }
}
