// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Represents the status of a request to update the heart beat for a message
    /// </summary>
    public interface IHeartBeatStatus
    {
        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        IMessageId MessageId { get;  }
        /// <summary>
        /// Gets the last heart beat time as UTC
        /// </summary>
        /// <remarks>If this value is null, we failed to update the heartbeat. This probably means that the record no longer exists, as an exception will be handled separately</remarks>
        /// <value>
        /// The last heart beat time.
        /// </value>
        DateTime? LastHeartBeatTime { get;  }
    }
}
