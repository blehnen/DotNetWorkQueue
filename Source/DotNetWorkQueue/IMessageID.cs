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
namespace DotNetWorkQueue
{
    /// <summary>
    /// A unique ID for a message in a particular queue
    /// </summary>
    /// <remarks>
    /// The ID is unique in a queue; however, it may not be unique between queues.
    /// Depending on the transport, the messageID may be reused again in the near or far future.
    /// See <see cref="ICorrelationId"/> for an ID that is unique
    /// </remarks>
    public interface IMessageId
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        ISetting Id { get; }
        /// <summary>
        /// Gets a value indicating if <see cref="Id"/> is not null / not empty
        /// </summary>
        /// <value>
        ///   <c>true</c> if <see cref="Id"/> has value; otherwise, <c>false</c>.
        /// </value>
        bool HasValue { get; }
    }
}
