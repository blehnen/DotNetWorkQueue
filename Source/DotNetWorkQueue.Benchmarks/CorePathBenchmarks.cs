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
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Decomposes the transport-independent cost of a send. Everything measured here is in the
    /// core library, so a change to any of it moves every transport rather than one.
    /// </summary>
    /// <remarks>
    /// The rungs are deliberately paired: the first of a pair is what the library does today and
    /// the second is the candidate replacement, both operating on the same input in the same
    /// process, so the difference between them is the change and nothing else.
    /// </remarks>
    [MemoryDiagnoser]
    public class CorePathBenchmarks
    {
        private const int PayloadBytes = 256;

        private MessageBody _message;
        private Dictionary<string, object> _headers;

        private JsonSerializerSettings _settings;
        private Newtonsoft.Json.JsonSerializer _cachedSerializer;
        private Type _bodyType;

        private ISerializer _newtonsoft;
        private ISerializer _systemTextJson;
        private MessageBody _messageBody;
        private byte[] _newtonsoftBytes;
        private byte[] _systemTextJsonBytes;
        private Dictionary<string, object> _emptyHeaders;

        [GlobalSetup]
        public void Setup()
        {
            _message = new MessageBody { Body = new Event { Body = new string('x', PayloadBytes) } };
            _headers = new Dictionary<string, object>
            {
                { "FirstPossibleDeliveryDate", DateTime.UtcNow },
                { "MessageBodyType", "DotNetWorkQueue.Benchmarks.CorePathBenchmarks+Event, DotNetWorkQueue.Benchmarks" }
            };

            //The same settings the shipped JsonSerializer builds.
            _settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = new NoOpBinder()
            };
            _cachedSerializer = Newtonsoft.Json.JsonSerializer.Create(_settings);
            _bodyType = typeof(Event);

            //the shipped serializers, resolved the way the container builds them
            var binder = new DenyListSerializationBinder();
            _newtonsoft = new DotNetWorkQueue.Serialization.JsonSerializer(binder);
            _systemTextJson = new SystemTextJsonSerializer(binder);
            _emptyHeaders = new Dictionary<string, object>();
            _messageBody = new MessageBody { Body = new Event { Body = new string('x', PayloadBytes) } };
            _newtonsoftBytes = _newtonsoft.ConvertMessageToBytes(_messageBody, _emptyHeaders);
            _systemTextJsonBytes = _systemTextJson.ConvertMessageToBytes(_messageBody, _emptyHeaders);
        }

        #region serializing the body

        /// <summary>What <see cref="JsonSerializer"/> does today.</summary>
        [Benchmark(Baseline = true, Description = "body: SerializeObject then UTF8.GetBytes (current)")]
        public int Body_Current()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_message, _settings)).Length;
        }

        /// <summary>
        /// The same, but with the <see cref="Newtonsoft.Json.JsonSerializer"/> built once instead of
        /// per call — <c>JsonConvert.SerializeObject</c> constructs one from the settings every time.
        /// Isolates that construction; the intermediate string is still allocated.
        /// </summary>
        [Benchmark(Description = "body: cached serializer, still via a string")]
        public int Body_CachedSerializer()
        {
            var sw = new StringWriter(new StringBuilder(256), System.Globalization.CultureInfo.InvariantCulture);
            using (var writer = new JsonTextWriter(sw))
            {
                _cachedSerializer.Serialize(writer, _message);
            }
            return Encoding.UTF8.GetBytes(sw.ToString()).Length;
        }

        /// <summary>
        /// Cached serializer writing UTF-8 straight into a stream — no intermediate string at all.
        /// Against the row above, this is what the string round trip costs.
        /// </summary>
        [Benchmark(Description = "body: cached serializer, direct to UTF8 bytes")]
        public int Body_DirectToBytes()
        {
            using var stream = new MemoryStream(512);
            using (var text = new StreamWriter(stream, Utf8NoBom, 1024, leaveOpen: true))
            using (var writer = new JsonTextWriter(text))
            {
                _cachedSerializer.Serialize(writer, _message);
            }
            return stream.ToArray().Length;
        }

        /// <summary>
        /// Cached serializer, string round trip kept, but the writer handed an array pool so its
        /// internal char buffers are rented rather than allocated.
        /// </summary>
        [Benchmark(Description = "body: cached serializer + pooled writer buffers")]
        public int Body_PooledBuffers()
        {
            var sb = new StringBuilder(512);
            var sw = new StringWriter(sb, System.Globalization.CultureInfo.InvariantCulture);
            using (var writer = new JsonTextWriter(sw) { ArrayPool = JsonArrayPool.Instance })
            {
                _cachedSerializer.Serialize(writer, _message);
            }
            return Encoding.UTF8.GetBytes(sw.ToString()).Length;
        }

        /// <summary>
        /// The floor: how many bytes the produced JSON actually is, so the allocation columns above
        /// can be read against the size of the thing being produced.
        /// </summary>
        [Benchmark(Description = "body: JSON payload size (reference, not a candidate)")]
        public int Body_PayloadSize()
        {
            return JsonConvert.SerializeObject(_message, _settings).Length;
        }

        #endregion

        #region serializing the headers

        /// <summary>What <see cref="JsonSerializerInternal"/> does today for the headers.</summary>
        [Benchmark(Description = "headers: SerializeObject then UTF8.GetBytes (current)")]
        public int Headers_Current()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_headers, _settings)).Length;
        }

        /// <summary>The header dictionary written straight to UTF-8 with a cached serializer.</summary>
        [Benchmark(Description = "headers: cached serializer, direct to UTF8 bytes")]
        public int Headers_DirectToBytes()
        {
            using var stream = new MemoryStream(256);
            using (var text = new StreamWriter(stream, Utf8NoBom, 1024, leaveOpen: true))
            using (var writer = new JsonTextWriter(text))
            {
                _cachedSerializer.Serialize(writer, _headers);
            }
            return stream.ToArray().Length;
        }

        #endregion

        #region the two shipped serializers, end to end

        /// <summary>
        /// The Newtonsoft serializer as registered by default, through its real public surface
        /// rather than a hand-rolled equivalent.
        /// </summary>
        [Benchmark(Description = "serializer: Newtonsoft, serialize")]
        public int Newtonsoft_Serialize()
            => _newtonsoft.ConvertMessageToBytes(_messageBody, _emptyHeaders).Length;

        /// <summary>The opt-in System.Text.Json serializer doing the same work.</summary>
        [Benchmark(Description = "serializer: System.Text.Json, serialize")]
        public int SystemTextJson_Serialize()
            => _systemTextJson.ConvertMessageToBytes(_messageBody, _emptyHeaders).Length;

        /// <summary>Reading a body back is the half that runs on every consumer.</summary>
        [Benchmark(Description = "serializer: Newtonsoft, deserialize")]
        public object Newtonsoft_Deserialize()
            => _newtonsoft.ConvertBytesToMessage<MessageBody>(_newtonsoftBytes, _emptyHeaders).Body;

        /// <summary>The same, through System.Text.Json.</summary>
        [Benchmark(Description = "serializer: System.Text.Json, deserialize")]
        public object SystemTextJson_Deserialize()
            => _systemTextJson.ConvertBytesToMessage<MessageBody>(_systemTextJsonBytes, _emptyHeaders).Body;

        #endregion

        #region the standard headers

        /// <summary>
        /// The portable body-type name <c>AddStandardMessageHeaders</c> builds on every send.
        /// <c>Assembly.GetName()</c> parses the assembly identity and allocates an
        /// <see cref="System.Reflection.AssemblyName"/> each call, for a value fixed per type.
        /// </summary>
        [Benchmark(Description = "header: portable type name, uncached (as it was)")]
        public string TypeName_Current()
        {
            return $"{_bodyType.FullName}, {_bodyType.Assembly.GetName().Name}";
        }

        /// <summary>The same value read from a per-type cache.</summary>
        [Benchmark(Description = "header: portable type name, cached (now)")]
        public string TypeName_Cached()
        {
            return PortableNames.GetOrAdd(_bodyType,
                static t => $"{t.FullName}, {t.Assembly.GetName().Name}");
        }

        #endregion

        #region argument validation

        /// <summary>
        /// The 14 <see cref="Guard"/> calls a single send makes, in the form the library used
        /// before: each took an <c>Expression&lt;Func&lt;T&gt;&gt;</c>, so the compiler built an
        /// expression tree at the call site and ran it on every call — including the call where
        /// nothing is wrong. Kept so the cost of going back to that form stays visible.
        /// </summary>
        [Benchmark(Description = "validation: 14x Guard.NotNull, expression tree (as it was)")]
        public object Guard_Expression()
        {
            object value = _message;
            for (var i = 0; i < 14; i++)
                Guard.NotNull(value);
            return value;
        }

        /// <summary>
        /// The same 14 checks in the form the library uses now: the parameter name arrives as a
        /// compile-time literal via <c>CallerArgumentExpression</c>, so the valid path is a null
        /// comparison and nothing else.
        /// </summary>
        [Benchmark(Description = "validation: 14x compiler-supplied name (now)")]
        public object Guard_Plain()
        {
            object value = _message;
            for (var i = 0; i < 14; i++)
                Guard.NotNull(value);
            return value;
        }

        #endregion

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        /// <summary>Hands Newtonsoft the shared <see cref="System.Buffers.ArrayPool{T}"/>.</summary>
        private sealed class JsonArrayPool : IArrayPool<char>
        {
            public static readonly JsonArrayPool Instance = new JsonArrayPool();
            public char[] Rent(int minimumLength) => System.Buffers.ArrayPool<char>.Shared.Rent(minimumLength);
            public void Return(char[] array) { if (array != null) System.Buffers.ArrayPool<char>.Shared.Return(array); }
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, string> PortableNames
            = new System.Collections.Concurrent.ConcurrentDictionary<Type, string>();

        /// <summary>A binder that does nothing, standing in for the configured allow/deny list.</summary>
        private sealed class NoOpBinder : ISerializationBinder
        {
            public Type BindToType(string assemblyName, string typeName) => null;
            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = serializedType.Assembly.FullName;
                typeName = serializedType.FullName;
            }
        }

        /// <summary>A message with a body large enough that serialization is not trivially free.</summary>
        public sealed class Event
        {
            public string Body { get; set; }
        }
    }
}
