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
using System.Text;
using DotNetWorkQueue.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DotNetWorkQueue.Serialization
{
    /// <summary>
    /// An opt-in serialization binder that only allows explicitly registered types to be deserialized.
    /// All types are blocked by default; callers must register each allowed type via <see cref="AddAllowedType(string)"/>.
    /// This binder is not registered by default. To use it, replace the default <see cref="DenyListSerializationBinder"/>
    /// in the DI container by registering this class as <see cref="ISerializationBinder"/> during transport initialization.
    /// This provides the strongest defense against JSON deserialization gadget attacks when
    /// <see cref="TypeNameHandling"/> is enabled.
    /// </summary>
    public class AllowListSerializationBinder : ISerializationBinder
    {
        private readonly HashSet<string> _allowedTypes = new HashSet<string>(StringComparer.Ordinal);
        private readonly DefaultSerializationBinder _defaultBinder = new DefaultSerializationBinder();

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowListSerializationBinder"/> class
        /// with an empty allow list. All types are blocked until explicitly registered
        /// via <see cref="AddAllowedType(string)"/>.
        /// </summary>
        public AllowListSerializationBinder()
        {
        }

        /// <summary>
        /// Adds a type name to the allow list. Subsequent attempts to deserialize this type will succeed.
        /// This method is not thread-safe with concurrent <see cref="BindToType"/> calls.
        /// Call during application startup before any deserialization occurs.
        /// </summary>
        /// <param name="typeName">The fully qualified type name to allow.</param>
        /// <exception cref="ArgumentNullException"><paramref name="typeName"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="typeName"/> is empty.</exception>
        public void AddAllowedType(string typeName)
        {
            Guard.NotNullOrEmpty(typeName);
            _allowedTypes.Add(CanonicalName(typeName));
        }

        /// <summary>
        /// Adds a type to the allow list using its <see cref="Type.FullName"/>.
        /// Subsequent attempts to deserialize this type will succeed.
        /// This method is not thread-safe with concurrent <see cref="BindToType"/> calls.
        /// Call during application startup before any deserialization occurs.
        /// </summary>
        /// <param name="type">The type to allow.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
        public void AddAllowedType(Type type)
        {
            Guard.NotNull(type);
            AddAllowedType(type.FullName);
        }

        /// <summary>
        /// Adds multiple type names to the allow list. Subsequent attempts to deserialize these types will succeed.
        /// This method is not thread-safe with concurrent <see cref="BindToType"/> calls.
        /// Call during application startup before any deserialization occurs.
        /// </summary>
        /// <param name="typeNames">The fully qualified type names to allow.</param>
        /// <exception cref="ArgumentNullException"><paramref name="typeNames"/> is null.</exception>
        public void AddAllowedTypes(IEnumerable<string> typeNames)
        {
            Guard.NotNull(typeNames);
            foreach (var typeName in typeNames)
            {
                AddAllowedType(typeName);
            }
        }

        /// <summary>
        /// Resolves a type name to a <see cref="Type"/> during deserialization.
        /// Throws <see cref="JsonSerializationException"/> if the type is not on the allow list.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <returns>The resolved <see cref="Type"/>.</returns>
        /// <exception cref="JsonSerializationException">The type is not on the allow list.</exception>
        public Type BindToType(string assemblyName, string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new JsonSerializationException("Deserialization requires a type name; none was given.");

            if (!_allowedTypes.Contains(CanonicalName(typeName)))
            {
                throw new JsonSerializationException(
                    $"Deserialization of type '{typeName}' is not allowed. " +
                    "This type is blocked by the allow-list binder because it has not been registered. " +
                    "Call AddAllowedType() to register types that are permitted for deserialization.");
            }

            return _defaultBinder.BindToType(assemblyName, typeName);
        }

        /// <summary>
        /// Gets the serialized type name and assembly for a given <see cref="Type"/>.
        /// Delegates to the default binder.
        /// </summary>
        /// <param name="serializedType">The type to get the name for.</param>
        /// <param name="assemblyName">The assembly name output.</param>
        /// <param name="typeName">The type name output.</param>
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            _defaultBinder.BindToName(serializedType, out assemblyName, out typeName);
        }

        /// <summary>
        /// The type name in a form that compares equal whichever serializer or runtime wrote it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="Type.FullName"/> qualifies generic arguments with version, culture and key
        /// (<c>List`1[[A.B, Asm, Version=1.0.0.0, ...]]</c>), while Json.NET writes simple names
        /// (<c>List`1[[A.B, Asm]]</c>), and .NET Framework names <c>mscorlib</c> where .NET names
        /// <c>System.Private.CoreLib</c>. Each generic argument keeps its simple assembly name, with the
        /// core library's names treated as one; versions, culture and key are dropped.
        /// </para>
        /// <para>
        /// The name is parsed, not scanned: a bracket after a type name is an argument list, either
        /// qualified (<c>[[T, Asm],[U, Asm]]</c>) or not (<c>[T,U]</c>), or an array rank (<c>[]</c>,
        /// <c>[,]</c>), and escaped characters (<c>\,</c>) are part of the name. A name that does not
        /// parse is returned as written, so it only ever matches itself, as before.
        /// </para>
        /// </remarks>
        internal static string CanonicalName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName) || typeName.IndexOf('[') < 0)
                return typeName;

            try
            {
                var position = 0;
                var result = new StringBuilder(typeName.Length);
                ParseType(typeName, ref position, result);
                return position == typeName.Length ? result.ToString() : typeName;
            }
            catch (FormatException)
            {
                return typeName;
            }
        }

        private static readonly HashSet<string> CoreLibraryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "System.Private.CoreLib", "mscorlib", "netstandard", "System.Runtime"
        };

        //a type: its name, then any generic arguments, then any array ranks
        private static void ParseType(string text, ref int position, StringBuilder result)
        {
            var start = result.Length;
            while (position < text.Length)
            {
                var c = text[position];
                if (c == '\\')
                {
                    if (position + 1 >= text.Length)
                        throw new FormatException("A name ends in an escape.");
                    result.Append(c).Append(text[position + 1]);
                    position += 2;
                    continue;
                }
                if (c == '[' || c == ']' || c == ',')
                    break;
                result.Append(c);
                position++;
            }
            if (result.Length == start)
                throw new FormatException("A type name is missing.");

            if (position + 1 < text.Length && text[position] == '[' && !IsArrayRank(text[position + 1]))
                ParseArguments(text, ref position, result);

            while (position < text.Length && text[position] == '[')
            {
                var close = position + 1;
                while (close < text.Length && IsArrayRank(text[close]) && text[close] != ']')
                    close++;
                if (close >= text.Length || text[close] != ']')
                    throw new FormatException("An array rank is not closed.");
                result.Append(text, position, close - position + 1);
                position = close + 1;
            }
        }

        //[[T, Asm],[U, Asm]] or [T,U]; written as [[T, Asm],[U]]
        private static void ParseArguments(string text, ref int position, StringBuilder result)
        {
            position++;
            result.Append('[');
            for (var first = true; ; first = false)
            {
                if (!first)
                    result.Append(',');
                SkipSpaces(text, ref position);
                result.Append('[');
                if (position < text.Length && text[position] == '[')
                {
                    position++;
                    ParseType(text, ref position, result);
                    SkipSpaces(text, ref position);
                    if (position < text.Length && text[position] == ',')
                    {
                        position++;
                        result.Append(", ").Append(AssemblyName(text, ref position));
                    }
                    Expect(text, ref position, ']');
                }
                else
                {
                    ParseType(text, ref position, result);
                }
                result.Append(']');

                SkipSpaces(text, ref position);
                if (position < text.Length && text[position] == ',')
                {
                    position++;
                    continue;
                }
                Expect(text, ref position, ']');
                result.Append(']');
                return;
            }
        }

        //an argument's assembly, up to the bracket that closes the argument: its simple name only
        private static string AssemblyName(string text, ref int position)
        {
            var start = position;
            while (position < text.Length && text[position] != ']')
            {
                if (text[position] == '[')
                    throw new FormatException("An assembly name contains '['.");
                position++;
            }
            var full = text.Substring(start, position - start);
            var comma = full.IndexOf(',');
            var simple = (comma < 0 ? full : full.Substring(0, comma)).Trim();
            if (simple.Length == 0)
                throw new FormatException("An assembly name is empty.");
            return CoreLibraryNames.Contains(simple) ? "System.Private.CoreLib" : simple;
        }

        private static bool IsArrayRank(char c)
        {
            return c == ']' || c == ',' || c == '*';
        }

        private static void SkipSpaces(string text, ref int position)
        {
            while (position < text.Length && text[position] == ' ')
                position++;
        }

        private static void Expect(string text, ref int position, char expected)
        {
            SkipSpaces(text, ref position);
            if (position >= text.Length || text[position] != expected)
                throw new FormatException($"Expected '{expected}'.");
            position++;
        }
    }
}
