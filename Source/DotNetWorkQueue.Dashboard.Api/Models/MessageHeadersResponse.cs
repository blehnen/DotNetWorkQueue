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
using System.Collections.Generic;

namespace DotNetWorkQueue.Dashboard.Api.Models
{
    /// <summary>
    /// Response model for deserialized message headers.
    /// </summary>
    public class MessageHeadersResponse
    {
        /// <summary>
        /// Gets or sets the message headers as a dictionary.
        /// </summary>
        public IDictionary<string, object> Headers { get; set; }

        /// <summary>
        /// Gets or sets an error message if header deserialization failed.
        /// </summary>
        public string DecodingError { get; set; }
    }
}
