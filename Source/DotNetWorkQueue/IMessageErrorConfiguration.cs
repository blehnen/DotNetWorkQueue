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
using System;
namespace DotNetWorkQueue
{
    /// <summary>
    /// Configuration settings for removing messages in an error status
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IMonitorTimespan" />
    /// <seealso cref="DotNetWorkQueue.IReadonly" />
    /// <seealso cref="DotNetWorkQueue.ISetReadonly" />
    public interface IMessageErrorConfiguration : IMonitorTimespan, IReadonly, ISetReadonly
    {
        /// <summary>
        /// If true, the queue will check for and delete messages that have an error status
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Messages that are older than this value, and are in an error status, will be deleted.
        /// </summary>
        /// <remarks>Age is based on the error time stamp, not the date the message was created</remarks>
        TimeSpan MessageAge { get; set; }
    }
}
