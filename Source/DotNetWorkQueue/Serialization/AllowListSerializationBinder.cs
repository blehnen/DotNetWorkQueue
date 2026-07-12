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
            Guard.NotNullOrEmpty(() => typeName, typeName);
            _allowedTypes.Add(typeName);
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
            Guard.NotNull(() => type, type);
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
            Guard.NotNull(() => typeNames, typeNames);
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
            if (!_allowedTypes.Contains(typeName))
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
    }
}
