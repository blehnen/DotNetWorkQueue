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
namespace DotNetWorkQueue.Dashboard.Api.Models
{
    /// <summary>
    /// Result codes for the edit-message-body operation.
    /// </summary>
    public enum EditMessageBodyResult
    {
        /// <summary>Body was re-encoded and saved successfully.</summary>
        Success,

        /// <summary>No message with the specified ID was found in the queue.</summary>
        NotFound,

        /// <summary>
        /// The message type could not be resolved (Queue-MessageBodyType header is absent or the
        /// assembly cannot be loaded). Edit is blocked because we cannot round-trip the body.
        /// </summary>
        TypeUnresolvable,

        /// <summary>
        /// The message is currently being processed (Status = 1). Edit is rejected to prevent
        /// corruption of an in-flight message.
        /// </summary>
        MessageBeingProcessed,

        /// <summary>
        /// The caller-supplied JSON could not be deserialized to the message's resolved type.
        /// </summary>
        InvalidJson
    }
}
