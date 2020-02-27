// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Collections.Concurrent;
using DotNetWorkQueue.Policies;
using Polly.Registry;

namespace DotNetWorkQueue
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPolicies
    {
        /// <summary>
        /// Gets the registry.
        /// </summary>
        /// <value>
        /// The registry.
        /// </value>
        PolicyRegistry Registry { get; }
        /// <summary>
        /// Gets the definition.
        /// </summary>
        /// <value>
        /// The definition.
        /// </value>
        PolicyDefinitions Definition { get; }

        /// <summary>
        /// Transport specific policies
        /// </summary>
        /// <value>
        /// The transport definition.
        /// </value>
        ConcurrentDictionary<string, TransportPolicyDefinition> TransportDefinition { get; }

        /// <summary>
        /// If true, failures will be inserted into polices
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable chaos]; otherwise, <c>false</c>.
        /// </value>
        bool EnableChaos { get; set; }
    }
}
