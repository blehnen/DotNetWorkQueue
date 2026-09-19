using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Basic
{
    /// <summary>
    /// A created PostgreSQL queue, its containers, and the few things a schema test needs to poke at
    /// it. Shared by the schema-version suites, which were each carrying their own copy.
    /// </summary>
    /// <remarks>
    /// Only the plumbing lives here: making the queue, resolving the updater, running SQL against it,
    /// and disposing all of it exactly once. What each suite does to the queue afterwards - dropping
    /// an index, converting columns back to a naive timestamp - stays with the test that cares,
    /// because that part is the test rather than its setup.
    ///
    /// Not shared with QueueNameIdentifierTests, which looks similar and is not: it must not create
    /// the queue in its constructor, because creating one is the thing it is testing.
    ///
    /// Not shared with the SQL Server and SQLite suites either. Their harnesses have the same shape,
    /// but the connection type, the catalog queries and the drop syntax all differ, and those
    /// differences are what those tests exist to exercise (GitHub #382).
    /// </remarks>
    internal sealed class PostgreSqlQueueHarness : IDisposable
    {
        private readonly QueueCreationContainer<PostgreSqlMessageQueueInit> _creationContainer;
        private readonly PostgreSqlMessageQueueCreation _creation;
        private readonly QueueContainer<PostgreSqlMessageQueueInit> _container;
        private readonly IContainer _admin;
        private bool _queueExists;

        /// <summary>
        /// Creates the queue and everything needed to inspect it.
        /// </summary>
        /// <param name="queueName">The queue name; generated when not given.</param>
        /// <param name="connectionSettings">Additional connection settings, for a test that needs one declared.</param>
        /// <param name="beforeCreate">Runs against the creation object before the queue is made, for options such as history.</param>
        public PostgreSqlQueueHarness(string queueName = null,
            IReadOnlyDictionary<string, string> connectionSettings = null,
            Action<PostgreSqlMessageQueueCreation> beforeCreate = null)
        {
            QueueName = queueName ?? GenerateQueueName.Create();
            var queueConnection = connectionSettings == null
                ? new QueueConnection(QueueName, ConnectionInfo.ConnectionString)
                : new QueueConnection(QueueName, ConnectionInfo.ConnectionString, connectionSettings);

            _creationContainer = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            try
            {
                _creation = _creationContainer.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);

                beforeCreate?.Invoke(_creation);

                var created = _creation.CreateQueue();
                Assert.IsTrue(created.Success, created.ErrorMessage);
                _queueExists = true;

                _container = new QueueContainer<PostgreSqlMessageQueueInit>();
                _admin = _container.CreateAdminContainer(queueConnection);
                Updater = _admin.GetInstance<IQueueSchemaVersion>();
            }
            catch
            {
                //A throw here means no instance reaches the caller, so their `using` never runs and
                //nothing else will clean up - not the containers, and not the queue if it was
                //already created. Failing to resolve the updater would otherwise leave a real queue
                //behind on the server.
                Cleanup();
                throw;
            }
        }

        /// <summary>The queue's name.</summary>
        public string QueueName { get; }

        /// <summary>The shipped schema updater for this queue.</summary>
        public IQueueSchemaVersion Updater { get; }

        /// <summary>
        /// Resolves a service from the queue's admin container.
        /// </summary>
        /// <remarks>
        /// Used for ITableNameHelper, so a test asks the library what a table is called rather than
        /// building the name itself and missing whatever schema the connection carries.
        /// </remarks>
        public T Resolve<T>() where T : class => _admin.GetInstance<T>();

        /// <summary>
        /// Leaves the queue looking like one created before schema versioning existed.
        /// </summary>
        public void DropSchemaVersionTable() => Execute($"drop table if exists {QueueName}SchemaVersion");

        /// <summary>
        /// Runs a statement against the queue's database.
        /// </summary>
        /// <remarks>
        /// Table names are interpolated by callers because an identifier cannot be a parameter in
        /// PostgreSQL. Values are bound - see <see cref="Text"/>.
        /// </remarks>
        public void Execute(string sql, params (string Name, object Value)[] parameters)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            Bind(command, parameters);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Reads a single string, binding any parameters given.
        /// </summary>
        public string Text(string sql, params (string Name, object Value)[] parameters)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            Bind(command, parameters);
            return command.ExecuteScalar() as string;
        }

        private static void Bind(System.Data.Common.DbCommand command,
            (string Name, object Value)[] parameters)
        {
            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }
        }

        /// <summary>
        /// Opens a connection to the queue's database, for a test that needs a reader.
        /// </summary>
        public NpgsqlConnection OpenConnection()
        {
            var connection = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            connection.Open();
            return connection;
        }

        public void Dispose() => Cleanup();

        /// <summary>
        /// Disposes whatever was acquired, and removes the queue if it got as far as existing.
        /// </summary>
        /// <remarks>
        /// Shared with the constructor's failure path, so a half-built harness is cleaned up the
        /// same way a used one is. Every field is null-checked because this runs at any point in
        /// construction.
        /// </remarks>
        private void Cleanup()
        {
            _admin?.Dispose();
            _container?.Dispose();
            try
            {
                if (_queueExists)
                    _creation.RemoveQueue();
            }
            finally
            {
                _creation?.Dispose();
                _creationContainer?.Dispose();
            }
        }
    }
}
