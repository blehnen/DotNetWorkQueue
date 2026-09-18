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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers
{
    /// <summary>
    /// Supplies the service endpoints this assembly runs against.
    /// </summary>
    /// <remarks>
    /// A connectionstring file in the output directory wins, which is how a developer points the
    /// suite at servers they already have. Otherwise each service starts its own container and is
    /// owned for the lifetime of the test process - see issue #281.
    ///
    /// Unlike the transport suites, this assembly covers several transports at once, so the
    /// containers are started lazily and independently. A run filtered to Memory, SQLite and LiteDb
    /// - which is what CLAUDE.md suggests for a machine with no services - starts none of them, and
    /// a run that only touches Redis pays for Redis alone. Tests run four at a time
    /// (see ParallelExecution.cs), so first use races and each endpoint is resolved under a lock.
    /// </remarks>
    public static class ConnectionStrings
    {
        /// <summary>
        /// Images match the servers these tests have always run against, so owning the instance is
        /// the only thing that changes.
        /// </summary>
        private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2019-latest";

        private const string PostgreSqlImage = "postgres:14";
        private const string RedisImage = "redis:8.10.1-alpine";

        private const string SqlServerDatabase = "IntegrationTests";
        private const string PostgreSqlDatabase = "integrationtesting";
        /// <summary>
        /// Generated per run. A container publishes a port on the docker host for the life of
        /// the test process, so a password committed to a public repository is one anyone who
        /// can reach that host could use while a build is running.
        /// </summary>
        private static readonly string ContainerPassword = $"Tc{Guid.NewGuid():N}!aA1";

        /// <summary>
        /// Set by CI for the stage that is supposed to own its services. A connectionstring file
        /// left in the output directory by an earlier build takes precedence over starting a
        /// container, and agents keep their workspace between builds - so without this a run could
        /// quietly go back to shared servers and still report every test as passing.
        /// </summary>
        private static bool RequireOwnContainer =>
            string.Equals(Environment.GetEnvironmentVariable("DNWQ_REQUIRE_SERVICE_CONTAINERS"),
                "true", StringComparison.OrdinalIgnoreCase);

        private static readonly Endpoint SqlServerEndpoint =
            new Endpoint("connectionstring.txt", StartSqlServer);

        private static readonly Endpoint PostgreSqlEndpoint =
            new Endpoint("connectionstring-postgresql.txt", StartPostgreSql);

        private static readonly Endpoint RedisEndpoint =
            new Endpoint("connectionstring-redis.txt", StartRedis);

        /// <summary>SQL Server, starting a container on first use if no file is present.</summary>
        public static string SqlServer => SqlServerEndpoint.Resolve(RequireOwnContainer);

        /// <summary>PostgreSQL, starting a container on first use if no file is present.</summary>
        public static string PostgreSql => PostgreSqlEndpoint.Resolve(RequireOwnContainer);

        /// <summary>Redis, starting a container on first use if no file is present.</summary>
        public static string Redis => RedisEndpoint.Resolve(RequireOwnContainer);

        /// <summary>
        /// Disposes whatever containers this process started. Called from assembly cleanup; the
        /// reaper would collect them anyway, but a SQL Server container is large enough that
        /// returning it promptly matters on an agent shared with other stages.
        /// </summary>
        public static void Shutdown()
        {
            SqlServerEndpoint.Shutdown();
            PostgreSqlEndpoint.Shutdown();
            RedisEndpoint.Shutdown();
        }

        private static (string, IAsyncDisposable) StartRedis() =>
            // The options these tests were always run with; StackExchange.Redis defaults
            // syncTimeout to 5000, which is below what the dashboard queries need here.
            StartAndInitialise(new RedisBuilder(RedisImage).Build(),
                c => c.GetConnectionString() + ",defaultDatabase=1,syncTimeout=15000");

        private static (string, IAsyncDisposable) StartPostgreSql()
        {
            // max_connections is raised to match the shared server: a stock image allows 100 and
            // the pool ceiling below is 250, with four tests running at once.
            var container = new PostgreSqlBuilder(PostgreSqlImage)
                .WithDatabase(PostgreSqlDatabase)
                .WithUsername("postgres")
                .WithPassword(ContainerPassword)
                .WithCommand("-c", "max_connections=500")
                .Build();
            return StartAndInitialise(container,
                c => c.GetConnectionString() +
                     ";Maximum Pool Size=250;Trust Server Certificate=true;Keepalive=15;Tcp Keepalive=true;");
        }

        private static (string, IAsyncDisposable) StartSqlServer()
        {
            var container = new MsSqlBuilder(SqlServerImage).WithPassword(ContainerPassword).Build();
            return StartAndInitialise(container, CreateDatabaseAndBuildConnectionString);
        }

        /// <summary>
        /// The container comes up with master only, so the database this suite names has to be
        /// created before anything connects to it.
        /// </summary>
        private static string CreateDatabaseAndBuildConnectionString(MsSqlContainer container)
        {
            var result = Task.Run(() => container.ExecScriptAsync(
                    $"IF DB_ID('{SqlServerDatabase}') IS NULL CREATE DATABASE [{SqlServerDatabase}];"))
                .GetAwaiter().GetResult();

            if (result.ExitCode != 0)
                throw new InvalidOperationException(
                    $"Could not create the {SqlServerDatabase} database in the SQL Server container: {result.Stderr}");

            var connectionString = new SqlConnectionStringBuilder(container.GetConnectionString())
            {
                InitialCatalog = SqlServerDatabase,
                ApplicationName = "IntegrationTesting",
                MaxPoolSize = 500,
                TrustServerCertificate = true
            }.ConnectionString;

            return connectionString;
        }

        /// <summary>
        /// Starts a container and prepares it, disposing it if either step fails. Without this a
        /// container that started but could not be initialised is left running and unreferenced:
        /// the caller never receives it, so assembly cleanup cannot dispose it, and the next test
        /// to ask for that service starts another one.
        /// </summary>
        private static (string, IAsyncDisposable) StartAndInitialise<TContainer>(
            TContainer container, Func<TContainer, string> initialise)
            where TContainer : DotNet.Testcontainers.Containers.IContainer
        {
            try
            {
                Task.Run(() => container.StartAsync()).GetAwaiter().GetResult();
                return (initialise(container), container);
            }
            catch
            {
                try
                {
                    container.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                catch
                {
                    // The original failure is the one worth reporting.
                }

                throw;
            }
        }

        /// <summary>
        /// One service endpoint: a file if there is one, otherwise a container this process owns.
        /// </summary>
        private sealed class Endpoint
        {
            private readonly object _sync = new object();
            private readonly string _fileName;
            private readonly Func<(string ConnectionString, IAsyncDisposable Container)> _start;
            private string _connectionString;
            private IAsyncDisposable _container;

            public Endpoint(string fileName, Func<(string, IAsyncDisposable)> start)
            {
                _fileName = fileName;
                _start = start;
            }

            public string Resolve(bool requireOwnContainer)
            {
                var resolved = Volatile.Read(ref _connectionString);
                if (!string.IsNullOrEmpty(resolved))
                    return resolved;

                lock (_sync)
                {
                    if (!string.IsNullOrEmpty(_connectionString))
                        return _connectionString;

                    var fromFile = ReadFile();
                    if (!string.IsNullOrEmpty(fromFile))
                    {
                        if (requireOwnContainer)
                            throw new InvalidOperationException(
                                $"DNWQ_REQUIRE_SERVICE_CONTAINERS is set, so this suite must start its own " +
                                $"service, but the endpoint came from {_fileName} in the output directory " +
                                $"instead. That file would send the run against a shared server while every " +
                                $"test still passed. See issue #281.");

                        _connectionString = fromFile;
                        return _connectionString;
                    }

                    var started = _start();

                    // Only now is the container usable, so only now does it become ours to dispose.
                    _container = started.Container;
                    _connectionString = started.ConnectionString;
                    return _connectionString;
                }
            }

            public void Shutdown()
            {
                IAsyncDisposable container;
                lock (_sync)
                {
                    container = _container;
                    _container = null;
                    _connectionString = null;
                }

                try
                {
                    container?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                catch
                {
                    // Nothing is waiting on this and the reaper collects whatever is left.
                }
            }

            private string ReadFile()
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), _fileName);
                return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
            }
        }

        public static string CreateSqliteInMemory(string queueName) =>
            $"FullUri=file:{queueName}?mode=memory&cache=shared;Version=3;";

        public static string LiteDbMemory => ":memory:";

        public static string Memory => "none";
    }
}
