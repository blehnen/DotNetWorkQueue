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
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using StackExchange.Redis;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Decomposes a single Redis de-queue, the other half of #233.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The empty-queue rungs are the ones that matter most. An idle consumer polls continuously,
    /// so whatever a de-queue costs when there is nothing to collect is paid forever by every
    /// queue that is not busy. That is where the SQLite pass found its largest win - generating
    /// the de-queue SQL was 91% of everything an empty de-queue allocated.
    /// </para>
    /// <para>
    /// Redis cannot have that particular problem: the de-queue is one fixed Lua script, loaded
    /// once and invoked by hash. These rungs are here to find out what it has instead, and to
    /// separate the part that is the client and the network from the part that is the library.
    /// </para>
    /// <para>
    /// The transport rungs resolve the real de-queue handler out of the consumer's own container,
    /// so they run with the decorators the library actually wires up rather than a hand-built
    /// approximation. See <see cref="ConsumerInternals"/>.
    /// </para>
    /// <para>
    /// <b>Requires a Redis instance</b> via <c>DNWQ_REDIS_CONNECTION</c>.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    [InvocationCount(16)]
    public class RedisReceiveBenchmarks
    {
        /// <summary>One more than the invocation count, so a populated rung never runs dry.</summary>
        private const int MessagesPerIteration = 24;
        private const int PayloadBytes = 256;

        private ConnectionMultiplexer _multiplexer;
        private IDatabase _database;
        private IServer _server;

        private string _rawPrefix;
        private RedisKey _rawValues;
        private RedisKey _rawHeaders;
        private RedisKey _rawPending;
        private RedisKey _rawWorking;
        private RedisKey _rawExpire;
        private RedisKey _rawStatus;
        private byte[] _scriptHash;
        private byte[] _body;

        private string _queueName;
        private QueueConnection _queueConnection;
        private QueueCreationContainer<RedisQueueInit> _creation;
        private QueueContainer<RedisQueueInit> _producerContainer;
        private IProducerQueue<Event> _producer;
        private QueueContainer<RedisQueueInit> _consumerContainer;
        private IConsumerQueue _consumer;
        private IQueryHandler<ReceiveMessageQuery, RedisMessage> _receive;
        private IMessageContextFactory _contextFactory;

        //an idle consumer polls a queue that stays empty. Sharing one queue with the populated
        //rung would have had the "empty" rung collecting the messages the setup just wrote.
        private QueueConnection _idleQueueConnection;
        private QueueContainer<RedisQueueInit> _idleConsumerContainer;
        private IConsumerQueue _idleConsumer;
        private IQueryHandler<ReceiveMessageQuery, RedisMessage> _receiveIdle;
        private IMessageContextFactory _idleContextFactory;
        private string _payload;

        /// <summary>
        /// The transport's de-queue script, written for <c>EVALSHA</c> with positional keys. Same
        /// work, called the cheapest way the client offers - the gap to the transport rung is the
        /// library rather than Redis.
        /// </summary>
        private const string DequeueScriptPositional = @"local uuid = redis.call('rpop', KEYS[1])
                    if (uuid==false) then
                        return nil;
                    end
                    local expireScore = redis.call('zscore', KEYS[5], uuid)
                    local message = redis.call('hget', KEYS[2], uuid)
                    local headers = redis.call('hget', KEYS[3], uuid)
                    if(message) then
                        redis.call('zadd', KEYS[4], ARGV[1], uuid)
                        redis.call('hset', KEYS[6], uuid, '1')
                        return {uuid, message, headers, expireScore}
                    else
                        return {uuid, '', '', ''}
                    end";

        [GlobalSetup]
        public void Setup()
        {
            var connectionString = Environment.GetEnvironmentVariable("DNWQ_REDIS_CONNECTION");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Set DNWQ_REDIS_CONNECTION to a Redis connection string. See the class remarks.");

            _body = new byte[PayloadBytes];
            _payload = new string('x', PayloadBytes);

            _multiplexer = ConnectionMultiplexer.Connect(connectionString);
            _database = _multiplexer.GetDatabase();
            _server = RedisPathBenchmarks.SingleServer(_multiplexer);

            var suffix = Guid.NewGuid().ToString("N");
            _rawPrefix = "{bench_" + suffix + "}";
            _rawPending = _rawPrefix + "pending";
            _rawValues = _rawPrefix + "values";
            _rawHeaders = _rawPrefix + "headers";
            _rawWorking = _rawPrefix + "working";
            _rawExpire = _rawPrefix + "expire";
            _rawStatus = _rawPrefix + "status";
            _scriptHash = _server.ScriptLoad(DequeueScriptPositional);

            _queueName = "bench" + suffix;
            _queueConnection = new QueueConnection(_queueName, connectionString);
            _creation = new QueueCreationContainer<RedisQueueInit>();
            using (var creator = _creation.GetQueueCreation<RedisQueueCreation>(_queueConnection))
            {
                var created = creator.CreateQueue();
                if (!created.Success)
                    throw new InvalidOperationException(created.ErrorMessage);
            }

            _producerContainer = new QueueContainer<RedisQueueInit>();
            _producer = _producerContainer.CreateProducer<Event>(_queueConnection);

            _consumerContainer = new QueueContainer<RedisQueueInit>();
            _consumer = _consumerContainer.CreateConsumer(_queueConnection);
            var container = ConsumerInternals.ContainerOf(_consumerContainer);
            _receive = container.GetInstance<IQueryHandler<ReceiveMessageQuery, RedisMessage>>();
            _contextFactory = container.GetInstance<IMessageContextFactory>();

            var idleQueueName = "benchidle" + suffix;
            _idleQueueConnection = new QueueConnection(idleQueueName, connectionString);
            using (var creator = _creation.GetQueueCreation<RedisQueueCreation>(_idleQueueConnection))
            {
                var created = creator.CreateQueue();
                if (!created.Success)
                    throw new InvalidOperationException(created.ErrorMessage);
            }

            _idleConsumerContainer = new QueueContainer<RedisQueueInit>();
            _idleConsumer = _idleConsumerContainer.CreateConsumer(_idleQueueConnection);
            var idleContainer = ConsumerInternals.ContainerOf(_idleConsumerContainer);
            _receiveIdle = idleContainer.GetInstance<IQueryHandler<ReceiveMessageQuery, RedisMessage>>();
            _idleContextFactory = idleContainer.GetInstance<IMessageContextFactory>();
        }

        /// <summary>
        /// The populated rungs consume what they read, so each iteration has to start from a known
        /// depth - otherwise a rung measures how many messages the one before it left behind.
        /// </summary>
        [IterationSetup]
        public void IterationSetup()
        {
            DeleteKeys(_rawPrefix + "*");
            DeleteKeys("*" + _queueName + "*");

            for (var i = 0; i < MessagesPerIteration; i++)
            {
                _database.ListLeftPush(_rawPending, i);
                _database.HashSet(_rawValues, i, _body);
                _database.HashSet(_rawHeaders, i, _body);

                var result = _producer.Send(new Event { Body = _payload });
                if (result.HasError) throw result.SendingException ?? new InvalidOperationException("send failed");
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _idleConsumer?.Dispose();
            _idleConsumerContainer?.Dispose();
            _consumer?.Dispose();
            _consumerContainer?.Dispose();
            _producer?.Dispose();
            _producerContainer?.Dispose();

            foreach (var connection in new[] { _queueConnection, _idleQueueConnection })
            {
                if (connection == null) continue;
                try
                {
                    using var creator = _creation.GetQueueCreation<RedisQueueCreation>(connection);
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
        /// The de-queue script against an empty pending list - the poll an idle consumer repeats
        /// forever, with the client cost and nothing else.
        /// </summary>
        [Benchmark(Baseline = true, Description = "raw: de-queue script, empty queue")]
        public object RawDequeue_Empty()
        {
            return _database.ScriptEvaluate(_scriptHash, EmptyKeys(), new RedisValue[] { 0 });
        }

        /// <summary>The same script when there is a message to collect.</summary>
        [Benchmark(Description = "raw: de-queue script, message waiting")]
        public object RawDequeue_Populated()
        {
            return _database.ScriptEvaluate(_scriptHash, RawKeys(), new RedisValue[] { 0 });
        }

        /// <summary>
        /// The library's de-queue against an empty queue, decorators included. Against the raw
        /// empty rung, what is left is what the library adds to a poll that collects nothing.
        /// </summary>
        [Benchmark(Description = "DotNetWorkQueue Redis de-queue, empty queue")]
        public object Transport_Dequeue_Empty()
        {
            using var context = _idleContextFactory.Create();
            return _receiveIdle.Handle(new ReceiveMessageQuery(context));
        }

        /// <summary>The library's de-queue when there is a message to collect.</summary>
        [Benchmark(Description = "DotNetWorkQueue Redis de-queue, message waiting")]
        public object Transport_Dequeue_Populated()
        {
            using var context = _contextFactory.Create();
            return _receive.Handle(new ReceiveMessageQuery(context));
        }

        /// <summary>Keys that exist but hold nothing, so the script takes its empty branch.</summary>
        private RedisKey[] EmptyKeys()
        {
            return new[] { (RedisKey)(_rawPrefix + "nothing"), _rawValues, _rawHeaders, _rawWorking, _rawExpire, _rawStatus };
        }

        private RedisKey[] RawKeys()
        {
            return new[] { _rawPending, _rawValues, _rawHeaders, _rawWorking, _rawExpire, _rawStatus };
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
