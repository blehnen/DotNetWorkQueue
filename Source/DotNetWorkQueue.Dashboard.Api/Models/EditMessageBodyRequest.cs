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
namespace DotNetWorkQueue.Dashboard.Api.Models
{
    /// <summary>
    /// Request body for the edit-message-body endpoint.
    /// </summary>
    public class EditMessageBodyRequest
    {
        /// <summary>
        /// Gets or sets the new message body as a JSON string. Must be deserializable to the
        /// original message type; the operation is rejected if the type cannot be resolved.
        /// </summary>
        public string Body { get; set; }
    }
}
