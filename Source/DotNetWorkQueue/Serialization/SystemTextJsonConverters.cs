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
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetWorkQueue.Validation;
using Newtonsoft.Json.Serialization;
//DotNetWorkQueue.Serialization.JsonSerializer shadows System.Text.Json.JsonSerializer inside this
//namespace, so the static entry points are reached through an alias.
using Stj = System.Text.Json.JsonSerializer;

namespace DotNetWorkQueue.Serialization
{
    /// <summary>
    /// Writes the runtime type of a message body alongside it, and resolves it on the way back.
    /// </summary>
    /// <remarks>
    /// Every dequeue asks for <c>BytesToMessage&lt;MessageBody&gt;</c> and reads
    /// <see cref="MessageBody.Body"/>, which is <c>dynamic</c> - the consumer's own message type
    /// never reaches the deserializer. So the runtime type has to come out of the payload.
    /// System.Text.Json will serialize an <see cref="object"/>-declared property using its runtime
    /// type, but deserializes one into a <c>JsonElement</c>, which makes an explicit type
    /// discriminator the whole ballgame.
    /// </remarks>
    internal sealed class MessageBodyConverter : JsonConverter<MessageBody>
    {
        private readonly ISerializationBinder _binder;

        /// <summary>Initializes a new instance of the <see cref="MessageBodyConverter"/> class.</summary>
        /// <param name="binder">Governs which types may be written and materialized.</param>
        public MessageBodyConverter(ISerializationBinder binder)
        {
            Guard.NotNull(binder);
            _binder = binder;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, MessageBody value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            object body = value?.Body;
            if (body == null)
            {
                writer.WriteNull(TypeProperty);
                writer.WriteEndObject();
                return;
            }

            var type = (Type)body.GetType();
            writer.WriteString(TypeProperty, TypeNaming.Write(_binder, type));
            writer.WritePropertyName(ValueProperty);
            Stj.Serialize(writer, body, type, options);
            writer.WriteEndObject();
        }

        /// <inheritdoc />
        public override MessageBody Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected an object for the message body.");

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != TypeProperty)
                throw new JsonException($"Expected '{TypeProperty}' as the first property of the message body.");

            reader.Read();
            if (reader.TokenType == JsonTokenType.Null)
            {
                reader.Read();
                return new MessageBody();
            }

            var type = TypeNaming.Read(_binder, reader.GetString());
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != ValueProperty)
                throw new JsonException($"Expected '{ValueProperty}' after the message body type.");

            reader.Read();
            var body = Stj.Deserialize(ref reader, type, options);
            reader.Read();
            return new MessageBody { Body = body };
        }

        internal const string TypeProperty = "$type";
        internal const string ValueProperty = "$value";
    }

    /// <summary>
    /// Handles polymorphism <em>inside</em> a message body: members declared as <see cref="object"/>,
    /// as an interface, or as an abstract class.
    /// </summary>
    /// <remarks>
    /// This cannot recurse. The factory only claims declared types that nothing can be a direct
    /// instance of, so re-serializing against the concrete runtime type always selects a different
    /// converter. Claiming concrete types as well would match Newtonsoft's
    /// <c>TypeNameHandling.Auto</c> more closely, but the converter would then re-enter itself for
    /// the very type it was writing.
    /// </remarks>
    internal sealed class PolymorphicMemberConverterFactory : JsonConverterFactory
    {
        private readonly ISerializationBinder _binder;

        /// <summary>Initializes a new instance of the <see cref="PolymorphicMemberConverterFactory"/> class.</summary>
        /// <param name="binder">Governs which types may be written and materialized.</param>
        public PolymorphicMemberConverterFactory(ISerializationBinder binder)
        {
            Guard.NotNull(binder);
            _binder = binder;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert == typeof(object)) return true;
            if (!typeToConvert.IsInterface && !typeToConvert.IsAbstract) return false;

            //System.Text.Json constructs the collection interfaces itself, and a caller may write a
            //concrete Dictionary while reading it back as IDictionary - claiming those would demand
            //a discriminator the write side never produced. Their elements are still declared
            //object, so they keep coming through here where it actually matters.
            return typeToConvert.Namespace != "System.Collections.Generic" &&
                   typeToConvert.Namespace != "System.Collections";
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(
                typeof(PolymorphicMemberConverter<>).MakeGenericType(typeToConvert), _binder);
        }
    }

    /// <summary>Writes and reads one polymorphic member. See <see cref="PolymorphicMemberConverterFactory"/>.</summary>
    /// <typeparam name="T">The declared type of the member.</typeparam>
    internal sealed class PolymorphicMemberConverter<T> : JsonConverter<T>
    {
        private readonly ISerializationBinder _binder;

        /// <summary>Initializes a new instance of the <see cref="PolymorphicMemberConverter{T}"/> class.</summary>
        /// <param name="binder">Governs which types may be written and materialized.</param>
        public PolymorphicMemberConverter(ISerializationBinder binder)
        {
            Guard.NotNull(binder);
            _binder = binder;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var type = value.GetType();
            writer.WriteStartObject();
            writer.WriteString(MessageBodyConverter.TypeProperty, TypeNaming.Write(_binder, type));
            writer.WritePropertyName(MessageBodyConverter.ValueProperty);
            Stj.Serialize(writer, value, type, options);
            writer.WriteEndObject();
        }

        /// <inheritdoc />
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return default;
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected an object for a member declared as {typeToConvert.Name}.");

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName ||
                reader.GetString() != MessageBodyConverter.TypeProperty)
                throw new JsonException($"Expected '{MessageBodyConverter.TypeProperty}' as the first property.");

            reader.Read();
            var type = TypeNaming.Read(_binder, reader.GetString());
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName ||
                reader.GetString() != MessageBodyConverter.ValueProperty)
                throw new JsonException($"Expected '{MessageBodyConverter.ValueProperty}' after the type.");

            reader.Read();
            var value = Stj.Deserialize(ref reader, type, options);
            reader.Read();
            return (T)value;
        }
    }

    /// <summary>
    /// Writes and resolves type names through the configured <see cref="ISerializationBinder"/>, so
    /// the allow or deny list governs the System.Text.Json path exactly as it governs Newtonsoft.
    /// </summary>
    internal static class TypeNaming
    {
        /// <summary>Renders a type as the string written into the payload.</summary>
        public static string Write(ISerializationBinder binder, Type type)
        {
            binder.BindToName(type, out var assemblyName, out var typeName);
            return string.IsNullOrEmpty(assemblyName) ? typeName : typeName + Separator + assemblyName;
        }

        /// <summary>Resolves a written type name back to a type.</summary>
        public static Type Read(ISerializationBinder binder, string stamped)
        {
            if (string.IsNullOrEmpty(stamped))
                throw new JsonException("The payload carries no type name.");

            var split = stamped.IndexOf(Separator);
            var type = split < 0
                ? binder.BindToType(null, stamped)
                : binder.BindToType(stamped.Substring(split + 1), stamped.Substring(0, split));

            if (type == null)
                throw new JsonException($"The type '{stamped}' could not be resolved.");

            return type;
        }

        //A vertical bar rather than a comma: an assembly qualified name already contains commas,
        //so splitting on one would need escaping that this does not.
        private const char Separator = '|';
    }
}
