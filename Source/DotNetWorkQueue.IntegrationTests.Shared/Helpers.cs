// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Collections.Concurrent;
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
            ConcurrentDictionary<Guid, bool> temp;
            RollBacks.TryRemove(queueId, out temp);
            temp?.Clear();

            IncrementWrapper temp2;
            Counters.TryRemove(queueId, out temp2);
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
        private ConcurrentDictionary<string, string> _ids;
        public IncrementWrapper()
        {
            ProcessedCount = 0;
            IdError = null;
            _ids = new ConcurrentDictionary<string, string>();
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

        public string IdError { get; private set; }
        public long ProcessedCount;
    }

    public class SerializerThatWillCrashOnDeSerialization : ISerializer
    {
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public byte[] ConvertMessageToBytes<T>(T message) where T : class
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, _serializerSettings));
        }

        public T ConvertBytesToMessage<T>(byte[] bytes) where T : class
        {
            // ReSharper disable once UnthrowableException
            throw new AccessViolationException("Permission denied");
        }
    }
}
