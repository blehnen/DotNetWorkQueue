// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Validation;
using Polly.Registry;

namespace DotNetWorkQueue.Policies
{
    /// <inheritdoc />
    public class Policies : IPolicies
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Policies"/> class.
        /// </summary>
        /// <param name="policyRegistry">The policy registry.</param>
        /// <param name="definitions">The send policy definitions.</param>
        public Policies(PolicyRegistry policyRegistry,
            PolicyDefinitions definitions)
        {
            Registry = policyRegistry;
            Definition = definitions;
            TransportDefinition = new ConcurrentDictionary<string, TransportPolicyDefinition>();
        }

        /// <inheritdoc />
        public PolicyRegistry Registry { get; }

        /// <inheritdoc />
        public PolicyDefinitions Definition { get; }

        /// <inheritdoc />
        public ConcurrentDictionary<string, TransportPolicyDefinition> TransportDefinition { get; }

        /// <inheritdoc />
        public bool EnableChaos { get; set; }
    }
}
