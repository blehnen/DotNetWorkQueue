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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Message expiration configuration settings for purging messages
    /// </summary>
    public interface IMessageExpirationConfiguration : IMonitorTimespan, IReadonly, ISetReadonly
    {
        /// <summary>
        /// If true, message expiration is supported by the transport.
        /// </summary>
        /// <value>
        /// <c>true</c> if [message expiration enabled]; otherwise, <c>false</c>.
        /// </value>
        bool Supported { get; }

        /// <summary>
        /// If true, the queue will check for and delete expired messages.
        /// </summary>
        /// <remarks>
        /// The transport must support expiration <see cref="Supported"/> for this setting to have any effect. 
        /// If the queue supports message expiration and this setting is false, it's up to some other process to remove expired messages.
        /// </remarks>
        /// <value>
        /// <c>true</c> if [clear expired messages enabled]; otherwise, <c>false</c>.
        /// </value>
        bool Enabled { get; set; }
    }
}
