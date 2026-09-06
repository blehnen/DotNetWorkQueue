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
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.Redis.Basic;
using StackExchange.Redis;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Decomposes a single Redis <c>Send</c>, the way <see cref="PostgreSqlPathBenchmarks"/> does
    /// for PostgreSQL. Opens the investigation in #233.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Redis is shaped differently from the relational transports, so the rungs ask different
    /// questions. There is no connection per operation, no SQL generation and no statement
    /// compilation - the three costs that dominated the SQLite pass. What is left is round trips,
    /// how the Lua script and its arguments are marshalled, and thread-pool behaviour.
    /// </para>
    /// <para>
    /// The ladder is built so that each gap isolates one of those:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>ping → separate commands</b> is what the round trips cost, and
    /// therefore what collapsing them into one script is worth. An enqueue is seven commands, so
    /// this rung is seven round trips against the one every rung below it makes.</description></item>
    /// <item><description><b>separate commands → script by hash</b> is that collapse, done the
    /// cheapest way the client offers: <c>EVALSHA</c> with explicit key and value arrays.</description></item>
    /// <item><description><b>script by hash → script with object parameters</b> is the cost of
    /// the way the transport actually calls it. <c>BaseLua</c> passes an anonymous object to
    /// <c>ScriptEvaluate</c>, so StackExchange.Redis maps <c>@name</c> placeholders onto members
    /// through its own extractor. Both rungs run the same script and do the same work in Redis,
    /// so the difference between them is marshalling and nothing else.</description></item>
    /// <item><description><b>script with object parameters → transport send</b> is the library:
    /// serialization, message construction, the decorator stack and the producer.</description></item>
    /// </list>
    /// <para>
    /// <b>Sync and async are reported separately, and the separation is the point.</b> Concurrent
    /// synchronous sends time out on StackExchange.Redis 3.x even when the connection pool is not
    /// starved - 3.0 removed its dedicated reply-completion pool, and <c>SetMinThreads</c> does not
    /// help. These rungs are single-threaded so they do not provoke it, but a sync number here
    /// must not be read as a per-operation cost that would hold at concurrency. See #161.
    /// </para>
    /// <para>
    /// <b>Requires a Redis instance</b> via <c>DNWQ_REDIS_CONNECTION</c>, on a database the harness
    /// may create and delete keys in.
    /// </para>
    /// <para>
    /// Every rung starts from an empty key set and an iteration is a fixed, small number of
    /// invocations - the same precaution the relational ladders document, where omitting it made
    /// the ladder measure the order it ran in rather than the work. Here it matters more than
    /// usual: the pending list and the values hash both grow per send, and <c>LPUSH</c> onto a
    /// long list is not what it is onto a short one.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    [InvocationCount(16)]
    public class RedisPathBenchmarks
    {
        private const int PayloadBytes = 256;

        private ConnectionMultiplexer _multiplexer;
        private IDatabase _database;
        private IServer _server;

        private byte[] _body;
        private byte[] _headers;
        private byte[] _meta;
        private string _payload;

        /// <summary>Raw-rung keys, in one hash slot the way the transport keeps its own.</summary>
        private string _rawPrefix;
        private RedisKey _rawValues;
        private RedisKey _rawHeaders;
        private RedisKey _rawPending;
        private RedisKey _rawMeta;
        private RedisKey _rawStatus;
        private RedisKey _rawId;
        private string _rawChannel;

        private byte[] _scriptHash;
        private LoadedLuaScript _loadedScript;

        private string _queueName;
        private QueueConnection _queueConnection;
        private QueueCreationContainer<RedisQueueInit> _creation;
        private QueueContainer<RedisQueueInit> _container;
        private IProducerQueue<Event> _producer;

        /// <summary>
        /// The transport's enqueue script with the job and route branches removed - an ordinary
        /// send takes neither, and leaving them in would measure two string comparisons per rung
        /// rather than the marshalling this is here to isolate.
        /// </summary>
        private const string EnqueueScript = @"local id = @field
                     if id == '' then
                        id = redis.call('INCR', @IDKey)
                     end
                     redis.call('hset', @key, id, @value)
                     redis.call('hset', @headerskey, id, @headers)
                     redis.call('lpush', @pendingkey, id)
                     redis.call('hset', @metakey, id, @metavalue)
                     redis.call('hset', @StatusKey, id, '0')
                     redis.call('publish', @channel, '')
                     return id";

        /// <summary>The same script written for <c>EVALSHA</c> with positional KEYS and ARGV.</summary>
        private const string EnqueueScriptPositional = @"local id = ARGV[1]
                     if id == '' then
                        id = redis.call('INCR', KEYS[6])
                     end
                     redis.call('hset', KEYS[1], id, ARGV[2])
                     redis.call('hset', KEYS[2], id, ARGV[3])
                     redis.call('lpush', KEYS[3], id)
                     redis.call('hset', KEYS[4], id, ARGV[4])
                     redis.call('hset', KEYS[5], id, '0')
                     redis.call('publish', ARGV[5], '')
                     return id";

        [GlobalSetup]
        public void Setup()
        {
            var connectionString = Environment.GetEnvironmentVariable("DNWQ_REDIS_CONNECTION");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Set DNWQ_REDIS_CONNECTION to a Redis connection string. See the class remarks.");

            _body = new byte[PayloadBytes];
            _headers = new byte[64];
            _meta = new byte[64];
            _payload = new string('x', PayloadBytes);

            _multiplexer = ConnectionMultiplexer.Connect(connectionString);
            _database = _multiplexer.GetDatabase();
            _server = SingleServer(_multiplexer);

            var suffix = Guid.NewGuid().ToString("N");
            _rawPrefix = "{bench_" + suffix + "}";
            _rawValues = _rawPrefix + "values";
            _rawHeaders = _rawPrefix + "headers";
            _rawPending = _rawPrefix + "pending";
            _rawMeta = _rawPrefix + "meta";
            _rawStatus = _rawPrefix + "status";
            _rawId = _rawPrefix + "id";
            _rawChannel = _rawPrefix + "notify";

            _scriptHash = _server.ScriptLoad(EnqueueScriptPositional);
            _loadedScript = LuaScript.Prepare(EnqueueScript).Load(_server);

            //the transport's own queue, for the end-to-end rungs
            _queueName = "bench" + suffix;
            _queueConnection = new QueueConnection(_queueName, connectionString);
            _creation = new QueueCreationContainer<RedisQueueInit>();
            using (var creator = _creation.GetQueueCreation<RedisQueueCreation>(_queueConnection))
            {
                var created = creator.CreateQueue();
                if (!created.Success)
                    throw new InvalidOperationException(created.ErrorMessage);
            }

            _container = new QueueContainer<RedisQueueInit>();
            _producer = _container.CreateProducer<Event>(_queueConnection);
        }

        /// <summary>
        /// An iteration must not pay for what the last one wrote. The pending list and the values
        /// hash both grow per send, and the raw rungs and the transport rungs write to different
        /// key sets, so both have to go.
        /// </summary>
        [IterationSetup]
        public void IterationSetup()
        {
            DeleteKeys(_rawPrefix + "*");
            DeleteKeys("*" + _queueName + "*");
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _producer?.Dispose();
            _container?.Dispose();

            if (_queueConnection != null)
            {
                try
                {
                    using var creator = _creation.GetQueueCreation<RedisQueueCreation>(_queueConnection);
                    creator.RemoveQueue();
                }
                catch (RedisException)
                {
                    //a queue left behind is not worth failing a run over
                }
            }
            _creation?.Dispose();

            try
            {
                DeleteKeys(_rawPrefix + "*");
            }
            catch (RedisException)
            {
                //same
            }

            _multiplexer?.Dispose();
        }

        /// <summary>The floor: one round trip that does no work.</summary>
        [Benchmark(Description = "round trip only: PING")]
        public TimeSpan Roundtrip_Ping()
        {
            return _database.Ping();
        }

        /// <summary>
        /// The writes an enqueue makes, as separate commands. Against the script rungs this is
        /// what collapsing them is worth.
        /// </summary>
        [Benchmark(Baseline = true, Description = "raw: DNWQ shape, separate commands")]
        public long RawWrites_SeparateCommands()
        {
            var id = _database.StringIncrement(_rawId);
            var field = (RedisValue)id;
            _database.HashSet(_rawValues, field, _body);
            _database.HashSet(_rawHeaders, field, _headers);
            _database.ListLeftPush(_rawPending, field);
            _database.HashSet(_rawMeta, field, _meta);
            _database.HashSet(_rawStatus, field, "0");
            _database.Publish(RedisChannel.Literal(_rawChannel), RedisValue.EmptyString);
            return id;
        }

        /// <summary>
        /// The same writes as one script, invoked by hash with positional keys and values - the
        /// cheapest way the client offers to say this.
        /// </summary>
        [Benchmark(Description = "raw: DNWQ shape, one script (EVALSHA, keys/values)")]
        public RedisResult RawWrites_OneScript_KeysAndValues()
        {
            var keys = new RedisKey[] { _rawValues, _rawHeaders, _rawPending, _rawMeta, _rawStatus, _rawId };
            var values = new RedisValue[] { RedisValue.EmptyString, _body, _headers, _meta, _rawChannel };
            return _database.ScriptEvaluate(_scriptHash, keys, values);
        }

        /// <summary>
        /// The same script and the same work, invoked the way the transport does it: an anonymous
        /// object whose members the client maps onto the <c>@name</c> placeholders. The gap to the
        /// rung above is argument marshalling, and nothing else.
        /// </summary>
        [Benchmark(Description = "raw: DNWQ shape, one script (object parameters, as the transport calls it)")]
        public RedisResult RawWrites_OneScript_ObjectParameters()
        {
            return _database.ScriptEvaluate(_loadedScript, new
            {
                field = RedisValue.EmptyString,
                key = _rawValues,
                value = (RedisValue)_body,
                headerskey = _rawHeaders,
                headers = (RedisValue)_headers,
                pendingkey = _rawPending,
                metakey = _rawMeta,
                metavalue = (RedisValue)_meta,
                StatusKey = _rawStatus,
                IDKey = _rawId,
                channel = _rawChannel
            });
        }

        /// <summary>
        /// The whole send, as a caller experiences it. Against the object-parameter rung, what is
        /// left is the library rather than the client or the server.
        /// </summary>
        [Benchmark(Description = "DotNetWorkQueue Redis send (end to end)")]
        public void Transport_Send()
        {
            var result = _producer.Send(new Event { Body = _payload });
            if (result.HasError) throw result.SendingException ?? new InvalidOperationException("send failed");
        }

        /// <summary>
        /// The asynchronous send. Reported separately because the synchronous path carries a
        /// StackExchange.Redis 3.x constraint the asynchronous one does not - see the remarks.
        /// </summary>
        [Benchmark(Description = "DotNetWorkQueue Redis SendAsync (end to end)")]
        public async Task Transport_SendAsync()
        {
            var result = await _producer.SendAsync(new Event { Body = _payload }).ConfigureAwait(false);
            if (result.HasError) throw result.SendingException ?? new InvalidOperationException("send failed");
        }

        /// <summary>
        /// The one server this harness supports, or a clear failure.
        /// </summary>
        /// <remarks>
        /// Scripts are loaded on one endpoint and keys are scanned from one endpoint. Against a
        /// cluster both are wrong: <c>EVALSHA</c> can route to a node that never loaded the
        /// script, and a scan misses the keys other nodes own - so the per-iteration cleanup would
        /// silently leave data behind and every later rung would measure a queue that grew. The
        /// second failure is the dangerous one, because it produces numbers rather than an error.
        /// Refusing to run is the honest response; this harness measures a single instance.
        /// </remarks>
        internal static IServer SingleServer(ConnectionMultiplexer multiplexer)
        {
            var endpoints = multiplexer.GetEndPoints();
            if (endpoints.Length != 1)
                throw new NotSupportedException(
                    $"These benchmarks measure a single Redis instance, and the connection names {endpoints.Length} " +
                    "endpoints. Scripts would be loaded on one node and keys scanned from one node, so the " +
                    "per-iteration cleanup would miss data and the numbers would drift without failing.");
            return multiplexer.GetServer(endpoints[0]);
        }

        private void DeleteKeys(string pattern)
        {
            foreach (var key in _server.Keys(_database.Database, pattern).ToArray())
            {
                _database.KeyDelete(key);
            }
        }

        public sealed class Event
        {
            public string Body { get; set; }
        }
    }
}
