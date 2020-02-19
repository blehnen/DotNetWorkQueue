using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    public static class MethodIncrementWrapper
    {
        private static readonly ConcurrentDictionary<Guid, IncrementWrapper> Counters;
        private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, bool>> RollBacks;
        static MethodIncrementWrapper()
        {
            Counters = new ConcurrentDictionary<Guid, IncrementWrapper>();
            RollBacks = new ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, bool>>();
        }

        public static void Clear(Guid queueId)
        {
            RollBacks.TryRemove(queueId, out var temp);
            temp?.Clear();

            Counters.TryRemove(queueId, out _);
        }
        public static void SetRollback(Guid queueId, Guid id)
        {
            if (!RollBacks.ContainsKey(queueId))
            {
                RollBacks.TryAdd(queueId, new ConcurrentDictionary<Guid, bool>());
            }
            RollBacks[queueId].TryAdd(id, true);
        }

        public static bool HasRollBack(Guid queueId, Guid id)
        {
            if (RollBacks.ContainsKey(queueId) && RollBacks[queueId].ContainsKey(id))
            {
                return RollBacks[queueId][id];
            }
            return false;
        }
        public static long Count(Guid queueId)
        {
            if (Counters.ContainsKey(queueId))
            {
                return Counters[queueId].ProcessedCount;
            }
            return 0;
        }
        public static void IncreaseCounter(Guid queueId)
        {
            if (!Counters.ContainsKey(queueId))
            {
                Counters.TryAdd(queueId, new IncrementWrapper());
            }

            var processor = Counters[queueId];
            Interlocked.Increment(ref processor.ProcessedCount);
        }
    }
    /// <summary>
    /// Allows easier re-usage of a byref counter
    /// </summary>
    public class IncrementWrapper
    {
        private readonly ConcurrentDictionary<string, string> _ids;
        private readonly ConcurrentDictionary<string, int> _errorCounts;

        public IncrementWrapper()
        {
            ProcessedCount = 0;
            IdError = null;
            _ids = new ConcurrentDictionary<string, string>();
            _errorCounts = new ConcurrentDictionary<string, int>();
        }

        public bool AddId(string id)
        {
            var result = _ids.TryAdd(id, null);
            if (!result)
            {
                IdError = id;
            }
            return result;
        }

        public void AddUpdateErrorCount(string id, int counter)
        {
            if (_errorCounts.ContainsKey(id))
                _errorCounts[id] = counter;
            else
                _errorCounts.TryAdd(id, counter);
        }

        public int GetErrorCount(string id)
        {
            if (_errorCounts.ContainsKey(id))
                return _errorCounts[id];
            return 0;
        }

        public string IdError { get; private set; }
        public long ProcessedCount;
    }

    public class SerializerThatWillCrashOnDeSerialization : ISerializer
    {
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public byte[] ConvertMessageToBytes<T>(T message, IReadOnlyDictionary<string, object> headers) where T : class
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, _serializerSettings));
        }

        public T ConvertBytesToMessage<T>(byte[] bytes, IReadOnlyDictionary<string, object> headers) where T : class
        {
            // ReSharper disable once UnthrowableException
            throw new AccessViolationException("Permission denied");
        }

        public string DisplayName => "WillCrash";
    }
}
