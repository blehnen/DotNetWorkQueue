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
using System.Collections.Generic;
using System.Text.Json;
using DotNetWorkQueue.Validation;
using Newtonsoft.Json.Serialization;
//DotNetWorkQueue.Serialization.JsonSerializer shadows System.Text.Json.JsonSerializer inside this
//namespace, so the static entry points are reached through an alias.
using Stj = System.Text.Json.JsonSerializer;

namespace DotNetWorkQueue.Serialization
{
    /// <summary>
    /// Serializes messages with <see cref="System.Text.Json"/> instead of Newtonsoft.
    /// </summary>
    /// <remarks>
    /// Opt in by registering this as <see cref="ISerializer"/> during transport initialization.
    /// Newtonsoft remains the default because it handles a wider range of arbitrary POCOs with no
    /// annotation, which is the shape this library has to support - callers queue whatever type
    /// they like. This is for callers who own their message types and want the allocation back:
    /// measured at 4,464 B against 752 B to serialize and 4,552 B against 1,272 B to read back, for
    /// a 256 byte message.
    /// <para>
    /// <b>Changing the serializer on a queue that already holds messages requires setting
    /// <see cref="ISerializerResolver.Fallback"/></b> to whatever wrote them. Messages written
    /// before the serializer header existed carry no marker, and the resolver has no way to guess.
    /// </para>
    /// <para>
    /// Two behaviour differences against the Newtonsoft serializer, both consequences of
    /// System.Text.Json rather than of this class:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// A property declared as a <em>concrete</em> base class but holding a derived instance loses
    /// the derived part. Newtonsoft writes a type marker whenever the runtime type differs from the
    /// declared one; System.Text.Json needs the derived types declared up front with
    /// <c>[JsonDerivedType]</c>. Properties declared as <see cref="object"/>, as an interface, or
    /// as an abstract class are handled here and need no annotation.
    /// </description></item>
    /// <item><description>
    /// Properties with private setters are not restored - though the Newtonsoft message serializer
    /// does not restore them either, so this matches existing behaviour.
    /// </description></item>
    /// </list>
    /// </remarks>
    public class SystemTextJsonSerializer : ISerializer
    {
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemTextJsonSerializer"/> class.
        /// </summary>
        /// <param name="serializationBinder">
        /// Controls which types may be materialized, exactly as it does for the Newtonsoft
        /// serializer - the same allow or deny list governs both.
        /// </param>
        public SystemTextJsonSerializer(ISerializationBinder serializationBinder)
        {
            Guard.NotNull(serializationBinder);
            _options = new JsonSerializerOptions
            {
                //Newtonsoft serializes public fields; System.Text.Json ignores them unless told to,
                //which would silently drop data on a POCO that uses them.
                IncludeFields = true,
                //The default, strict encoder is kept deliberately. It escapes '+', '<', '>' and '&',
                //which costs a few bytes on nested and generic type names, and is why a payload here
                //is slightly larger than the Newtonsoft equivalent. UnsafeRelaxedJsonEscaping would
                //recover those bytes, but message bodies are rendered by the dashboard, and relaxing
                //HTML escaping on content that reaches a web UI is not a trade worth making for the
                //size of a type name.
                Converters =
                {
                    new MessageBodyConverter(serializationBinder),
                    new PolymorphicMemberConverterFactory(serializationBinder)
                }
            };
            DisplayName = "System.Text.Json";
        }

        /// <inheritdoc />
        public byte[] ConvertMessageToBytes<T>(T message, IReadOnlyDictionary<string, object> headers)
            where T : class
        {
            Guard.NotNull(message);
            return Stj.SerializeToUtf8Bytes(message, _options);
        }

        /// <inheritdoc />
        public T ConvertBytesToMessage<T>(byte[] bytes, IReadOnlyDictionary<string, object> headers)
            where T : class
        {
            Guard.NotNull(bytes);
            return Stj.Deserialize<T>(bytes, _options);
        }

        /// <inheritdoc />
        public string DisplayName { get; }

        /// <inheritdoc />
        /// <remarks>
        /// Constant rather than derived from the type: this goes on the wire, and a rename must not
        /// strand messages already sitting in a queue.
        /// </remarks>
        public string SerializerId => Id;

        /// <summary>The wire identifier for this serializer.</summary>
        public const string Id = "system.text.json";
    }
}
