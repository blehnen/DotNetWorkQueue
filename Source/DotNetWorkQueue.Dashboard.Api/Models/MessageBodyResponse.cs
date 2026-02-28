// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Collections.Generic;

namespace DotNetWorkQueue.Dashboard.Api.Models
{
    /// <summary>
    /// Response model for a decoded message body.
    /// </summary>
    public class MessageBodyResponse
    {
        /// <summary>
        /// Gets or sets the decoded message body as a JSON string. Null if decoding failed.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the fully-qualified .NET type name of the decoded message body (e.g. "MyApp.MyMessage, MyApp").
        /// Null if decoding failed or the type could not be determined.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether interceptors (compression/encryption) were applied and reversed.
        /// </summary>
        public bool WasIntercepted { get; set; }

        /// <summary>
        /// Gets or sets the list of interceptor type names that were applied, in original order.
        /// </summary>
        public IReadOnlyList<string> InterceptorChain { get; set; }

        /// <summary>
        /// Gets or sets an error message if decoding failed (e.g., missing encryption keys).
        /// </summary>
        public string DecodingError { get; set; }
    }
}
