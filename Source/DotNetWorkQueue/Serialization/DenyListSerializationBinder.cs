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
    /// A serialization binder that blocks known dangerous types from being deserialized.
    /// This provides defense-in-depth against JSON deserialization gadget attacks when
    /// <see cref="TypeNameHandling"/> is enabled.
    /// </summary>
    public class DenyListSerializationBinder : ISerializationBinder
    {
        private readonly HashSet<string> _deniedTypes;
        private readonly DefaultSerializationBinder _defaultBinder = new DefaultSerializationBinder();

        /// <summary>
        /// Initializes a new instance of the <see cref="DenyListSerializationBinder"/> class
        /// with the default set of known dangerous gadget types.
        /// </summary>
        public DenyListSerializationBinder()
        {
            _deniedTypes = GetDefaultDeniedTypes();
        }

        /// <summary>
        /// Adds a type name to the deny list. Subsequent attempts to deserialize this type will throw.
        /// </summary>
        /// <param name="typeName">The fully qualified type name to deny.</param>
        /// <exception cref="ArgumentNullException"><paramref name="typeName"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="typeName"/> is empty.</exception>
        public void AddDeniedType(string typeName)
        {
            Guard.NotNullOrEmpty(() => typeName, typeName);
            _deniedTypes.Add(typeName);
        }

        /// <summary>
        /// Adds multiple type names to the deny list. Subsequent attempts to deserialize these types will throw.
        /// </summary>
        /// <param name="typeNames">The fully qualified type names to deny.</param>
        /// <exception cref="ArgumentNullException"><paramref name="typeNames"/> is null.</exception>
        public void AddDeniedTypes(IEnumerable<string> typeNames)
        {
            Guard.NotNull(() => typeNames, typeNames);
            foreach (var typeName in typeNames)
            {
                AddDeniedType(typeName);
            }
        }

        /// <summary>
        /// Resolves a type name to a <see cref="Type"/> during deserialization.
        /// Throws <see cref="JsonSerializationException"/> if the type is on the deny list.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <returns>The resolved <see cref="Type"/>.</returns>
        /// <exception cref="JsonSerializationException">The type is on the deny list.</exception>
        public Type BindToType(string assemblyName, string typeName)
        {
            if (_deniedTypes.Contains(typeName))
            {
                throw new JsonSerializationException(
                    $"Deserialization of type '{typeName}' is not allowed. " +
                    "This type is on the deny list because it is a known deserialization gadget.");
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
        /// Returns the default set of denied types that are known deserialization gadgets.
        /// </summary>
        /// <returns>A <see cref="HashSet{String}"/> containing the denied type names.</returns>
        private static HashSet<string> GetDefaultDeniedTypes()
        {
            return new HashSet<string>(StringComparer.Ordinal)
            {
                "System.Windows.Data.ObjectDataProvider",
                "System.Security.Principal.WindowsIdentity",
                "System.IO.FileInfo",
                "System.IO.DirectoryInfo",
                "System.Diagnostics.Process",
                "System.Configuration.Install.AssemblyInstaller",
                "System.Activities.Presentation.WorkflowDesigner",
                "System.Windows.ResourceDictionary",
                "System.Windows.Forms.BindingSource",
                "System.Web.Security.RolePrincipal",
                "Microsoft.VisualStudio.Text.Formatting.TextFormattingRunProperties",
                "System.IdentityModel.Tokens.SessionSecurityToken",
                "System.Security.Claims.ClaimsIdentity",
                "System.Security.Claims.ClaimsPrincipal",
                "System.Data.DataSet",
                "System.Data.DataTable",
                "System.Xml.XmlDocument",
                "System.Xml.XmlDataDocument",
                "System.Management.Automation.PSObject",
                "System.Runtime.Serialization.Formatters.Soap.SoapFormatter"
            };
        }
    }
}
