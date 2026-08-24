using System.Data;
using System.Data.SQLite;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.SQLite.Basic;
using MemoryBasic = DotNetWorkQueue.Transport.Memory.Basic;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Decomposes the cost of a single SQLite <c>Send</c> into its parts, so optimisation work can
    /// be aimed at measured cost rather than at what looks expensive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each benchmark isolates one layer. Read them as a ladder: the difference between two
    /// adjacent rungs is the cost of what the upper rung adds.
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="RawTable_OneStatement"/> — the floor: what a hand-written
    /// narrow outbox table costs.</description></item>
    /// <item><description><see cref="RawTable_DnwqShape_HeldConnection"/> — the same table put
    /// through DotNetWorkQueue's statement shape (two tables, three statements, the separate
    /// <c>last_insert_rowid()</c> round trip). Minus the row above, this is the cost of the write
    /// transaction — the "critical section".</description></item>
    /// <item><description><see cref="RawTable_DnwqShape_ConnectionPerSend"/> — the same again but
    /// acquiring a pooled connection per send, as the transport does. Minus the row above, this is
    /// the cost of connection lifecycle.</description></item>
    /// <item><description><see cref="MemoryTransport_Send"/> — the producer pipeline with no
    /// serialization and no SQL. The transport-independent floor.</description></item>
    /// <item><description><see cref="Serializer_BodyAndHeaders"/> — the real configured
    /// serializer, doing exactly what the send path does.</description></item>
    /// <item><description><see cref="DatabaseExists_Check"/> — the existence check the send path
    /// performs per message.</description></item>
    /// <item><description><see cref="Sqlite_Send"/> — the whole thing.</description></item>
    /// </list>
    /// <para>
    /// A scratch version of this decomposition (2026-08-24, net10) found the write transaction to
    /// be about 3% of the gap and connection lifecycle about 58%, which falsified the assumption
    /// that shortening the critical section was the useful target. That run had a ±25% band; this
    /// harness exists to replace it with numbers precise enough to act on.
    /// </para>
    /// <para>
    /// <c>synchronous</c> is a per-connection pragma and, unlike <c>journal_mode</c>, is not
    /// persisted in the database file. Every connection string here sets it explicitly; omitting
    /// it silently reverts a connection to <c>FULL</c> and buys an fsync that the comparison rows
    /// do not pay.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class SendPathBenchmarks
    {
        private const string Sync = "NORMAL";
        private const int PayloadBytes = 256;

        private string _dir;
        private string _rawPath;
        private string _shapePath;
        private string _shapePerSendPath;

        private SQLiteConnection _rawConnection;
        private SQLiteConnection _shapeConnection;
        private string _shapePerSendConnectionString;

        private QueueCreationContainer<SqLiteMessageQueueInit> _sqliteCreation;
        private QueueContainer<SqLiteMessageQueueInit> _sqliteContainer;
        private IProducerQueue<Event> _sqliteProducer;

        private QueueCreationContainer<MemoryBasic.MemoryMessageQueueInit> _memoryCreation;
        private QueueContainer<MemoryBasic.MemoryMessageQueueInit> _memoryContainer;
        private IProducerQueue<Event> _memoryProducer;

        private ICompositeSerialization _serializer;
        private DatabaseExists _databaseExists;
        private string _sqliteConnectionString;

        private string _payload;
        private byte[] _body;
        private byte[] _headerBytes;
        private Dictionary<string, object> _headers;

        /// <summary>A message with a body large enough that serialization is not trivially free.</summary>
        public sealed class Event
        {
            public string Body { get; set; }
        }

        //Setup is scoped per benchmark rather than shared. BenchmarkDotNet runs each benchmark in
        //its own process, so a single [GlobalSetup] would make every process build every fixture -
        //including a full queue and a Memory transport - for a benchmark that only opens a
        //connection. That is both wasteful and a confound: it puts unrelated open databases and
        //live producers in the process being measured.

        private void SetupCommon()
        {
            _payload = new string('x', PayloadBytes);
            _body = Encoding.UTF8.GetBytes(_payload);
            _headerBytes = new byte[64];
            _headers = new Dictionary<string, object>();

            _dir = Path.Combine(Path.GetTempPath(), "dnwq-benchmarks", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [GlobalSetup(Target = nameof(RawTable_OneStatement))]
        public void SetupForRawTable()
        {
            SetupCommon();
            SetupRawTable();
        }

        [GlobalSetup(Target = nameof(RawTable_DnwqShape_HeldConnection))]
        public void SetupForShapeHeld()
        {
            SetupCommon();
            _shapePath = Path.Combine(_dir, "shape.db");
            _shapeConnection = Open(_shapePath, pooling: false);
            Execute(_shapeConnection, ShapeSchema);
        }

        [GlobalSetup(Targets = new[] { nameof(RawTable_DnwqShape_ConnectionPerSend), nameof(PooledConnection_OpenClose) })]
        public void SetupForShapePerSend()
        {
            SetupCommon();
            _shapePerSendPath = Path.Combine(_dir, "shape-persend.db");
            _shapePerSendConnectionString = ConnectionString(_shapePerSendPath, pooling: true);
            using var seed = Open(_shapePerSendPath, pooling: true);
            Execute(seed, ShapeSchema);
        }

        [GlobalSetup(Targets = new[] { nameof(Sqlite_Send), nameof(Serializer_BodyAndHeaders), nameof(DatabaseExists_Check) })]
        public void SetupForSqlite()
        {
            SetupCommon();
            SetupSqlite();
        }

        [GlobalSetup(Target = nameof(MemoryTransport_Send))]
        public void SetupForMemory()
        {
            SetupCommon();
            SetupMemory();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _sqliteProducer?.Dispose();
            _sqliteContainer?.Dispose();
            _sqliteCreation?.Dispose();
            _memoryProducer?.Dispose();
            _memoryContainer?.Dispose();
            _memoryCreation?.Dispose();
            _rawConnection?.Dispose();
            _shapeConnection?.Dispose();

            //pooled connections hold the file handles open
            SQLiteConnection.ClearAllPools();
            try { Directory.Delete(_dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        // ---------------------------------------------------------------- the ladder

        /// <summary>The floor: one statement, one commit, on a connection that is already open.</summary>
        [Benchmark(Baseline = true, Description = "raw table, 1 statement")]
        public void RawTable_OneStatement()
        {
            using var tx = _rawConnection.BeginTransaction();
            using var cmd = _rawConnection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO outbox(body, created_utc) VALUES (@b, @d)";
            cmd.Parameters.Add("@b", DbType.String).Value = _payload;
            cmd.Parameters.Add("@d", DbType.Int64).Value = DateTime.UtcNow.Ticks;
            cmd.ExecuteNonQuery();
            tx.Commit();
        }

        /// <summary>
        /// Minus <see cref="RawTable_OneStatement"/>, this is the cost of the write transaction the
        /// transport holds: the extra table, the extra statement, and the id round trip.
        /// </summary>
        [Benchmark(Description = "raw table, DNWQ statement shape (held connection)")]
        public void RawTable_DnwqShape_HeldConnection() => DnwqShape(_shapeConnection);

        /// <summary>
        /// Minus <see cref="RawTable_DnwqShape_HeldConnection"/>, this is the cost of acquiring and
        /// releasing a pooled connection per send — what the transport does today.
        /// </summary>
        [Benchmark(Description = "raw table, DNWQ shape + pooled connection per send")]
        public void RawTable_DnwqShape_ConnectionPerSend()
        {
            using var connection = new SQLiteConnection(_shapePerSendConnectionString);
            connection.Open();
            DnwqShape(connection);
        }

        /// <summary>Connection acquisition alone, with no work done on it.</summary>
        [Benchmark(Description = "pooled connection open + close, no work")]
        public void PooledConnection_OpenClose()
        {
            using var connection = new SQLiteConnection(_shapePerSendConnectionString);
            connection.Open();
        }

        /// <summary>The existence check <c>SendMessageCommandHandler</c> runs on every send.</summary>
        [Benchmark(Description = "DatabaseExists check (once per send)")]
        public bool DatabaseExists_Check() => _databaseExists.Exists(_sqliteConnectionString);

        /// <summary>
        /// The real configured serializer, doing exactly what <c>GetMainCommand</c> does: the body
        /// through the interceptor graph, then the headers through the internal serializer.
        /// </summary>
        [Benchmark(Description = "serialize body + headers")]
        public int Serializer_BodyAndHeaders()
        {
            var result = _serializer.Serializer.MessageToBytes(
                new MessageBody { Body = new Event { Body = _payload } }, _headers);
            var headers = _serializer.InternalSerializer.ConvertToBytes(_headers);
            return result.Output.Length + headers.Length;
        }

        /// <summary>
        /// The producer pipeline with no serialization and no SQL — the Memory transport stores the
        /// POCO directly. Whatever this costs cannot be fixed in a transport.
        /// </summary>
        [Benchmark(Description = "core producer pipeline (Memory transport)")]
        public void MemoryTransport_Send()
        {
            var result = _memoryProducer.Send(new Event { Body = _payload });
            if (result.HasError) throw result.SendingException ?? new Exception("send failed");
        }

        /// <summary>The whole send path, as a caller experiences it.</summary>
        [Benchmark(Description = "DotNetWorkQueue SQLite send (end to end)")]
        public void Sqlite_Send()
        {
            var result = _sqliteProducer.Send(new Event { Body = _payload });
            if (result.HasError) throw result.SendingException ?? new Exception("send failed");
        }

        // ---------------------------------------------------------------- setup

        private void DnwqShape(SQLiteConnection connection)
        {
            using var tx = connection.BeginTransaction();
            long id;
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "Insert into q (Body, Headers) VALUES (@Body, @Headers); ";
                cmd.Parameters.Add("@Body", DbType.Binary).Value = _body;
                cmd.Parameters.Add("@Headers", DbType.Binary).Value = _headerBytes;
                cmd.ExecuteNonQuery();
            }
            using (var cmd = connection.CreateCommand())
            {
                //the separate round trip the transport pays to recover the generated id
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT last_insert_rowid();";
                id = Convert.ToInt64(cmd.ExecuteScalar());
            }
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText =
                    "Insert into qmeta (QueueID, CorrelationID, QueuedDateTime) VALUES (@QueueID, @CorrelationID, @CurrentDate)";
                cmd.Parameters.Add("@QueueID", DbType.Int64).Value = id;
                cmd.Parameters.Add("@CorrelationID", DbType.String).Value = Guid.NewGuid().ToString();
                cmd.Parameters.Add("@CurrentDate", DbType.Int64).Value = DateTime.UtcNow.Ticks;
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }

        private void SetupRawTable()
        {
            _rawPath = Path.Combine(_dir, "raw.db");
            _rawConnection = Open(_rawPath, pooling: false);
            Execute(_rawConnection,
                "CREATE TABLE outbox(id INTEGER PRIMARY KEY AUTOINCREMENT, body TEXT NOT NULL, created_utc INTEGER NOT NULL);");
        }

        private const string ShapeSchema =
            "CREATE TABLE q(QueueID INTEGER PRIMARY KEY AUTOINCREMENT, Body BLOB, Headers BLOB);" +
            "CREATE TABLE qmeta(QueueID INTEGER PRIMARY KEY, CorrelationID TEXT, QueuedDateTime INTEGER);";

        private void SetupSqlite()
        {
            var path = Path.Combine(_dir, "dnwq.db");
            //the transport adds Pooling itself; journal_mode=WAL is its default
            _sqliteConnectionString = $"Data Source={path};Version=3;Synchronous={Sync};";
            var connection = new QueueConnection("benchSend", _sqliteConnectionString);

            _sqliteCreation = new QueueCreationContainer<SqLiteMessageQueueInit>();
            using (var creator = _sqliteCreation.GetQueueCreation<SqLiteMessageQueueCreation>(connection))
            {
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"CreateQueue failed: {result.Status} {result.ErrorMessage}");
            }

            _sqliteContainer = new QueueContainer<SqLiteMessageQueueInit>();
            _sqliteProducer = _sqliteContainer.CreateProducer<Event>(connection);

            _serializer = ReflectSerializer(_sqliteProducer);
            _databaseExists = new DatabaseExists(new GetFileNameFromConnectionString());
        }

        private void SetupMemory()
        {
            var connection = new QueueConnection("benchSendMemory", "memory");
            _memoryCreation = new QueueCreationContainer<MemoryBasic.MemoryMessageQueueInit>();
            using (var creator = _memoryCreation.GetQueueCreation<MemoryBasic.MessageQueueCreation>(connection))
            {
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"memory CreateQueue failed: {result.Status}");
            }
            _memoryContainer = new QueueContainer<MemoryBasic.MemoryMessageQueueInit>();
            _memoryProducer = _memoryContainer.CreateProducer<Event>(connection);
        }

        /// <summary>
        /// Pulls the live serializer out of a running producer, so the benchmark measures the
        /// configured instances with their decorators and interceptor graph rather than a
        /// hand-built approximation that could drift from what the send path actually uses.
        /// </summary>
        private static ICompositeSerialization ReflectSerializer(IProducerQueue<Event> producer)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            object Field(object target, string name) =>
                target?.GetType().GetField(name, flags)?.GetValue(target);
            object Unwrap(object target) =>
                Field(target, "_handler") ?? Field(target, "_decorated") ?? Field(target, "_inner");

            var node = Field(producer, "_sendMessages");
            for (var i = 0; node != null && i < 12 && Field(node, "_sendMessage") == null; i++)
                node = Unwrap(node);

            var handler = Field(node, "_sendMessage");
            for (var i = 0; handler != null && i < 12 && Field(handler, "_serializer") == null; i++)
                handler = Unwrap(handler);

            return Field(handler, "_serializer") as ICompositeSerialization
                   ?? throw new InvalidOperationException(
                       "Could not reach the serializer through the send chain. The decorator " +
                       "layout or field names changed; update ReflectSerializer.");
        }

        private static string ConnectionString(string path, bool pooling) =>
            $"Data Source={path};Version=3;Synchronous={Sync};" + (pooling ? "Pooling=True;" : string.Empty);

        private static SQLiteConnection Open(string path, bool pooling)
        {
            var connection = new SQLiteConnection(ConnectionString(path, pooling));
            connection.Open();
            Execute(connection, $"PRAGMA journal_mode=WAL;PRAGMA synchronous={Sync};");
            return connection;
        }

        private static void Execute(SQLiteConnection connection, string sql)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
