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

namespace DotNetWorkQueue
{
    /// <summary>
    /// Updates the heartbeat flag for a message that is either
    /// 1. Being processed
    /// 2. Has been read from the queue, but is now in the in memory queue and waiting to start processing (async consumer only)
    /// </summary>
    public interface IHeartBeatWorker: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops this instance.
        /// </summary>
        /// <remarks>
        /// Stop is explicitly called when an error occurs, so that we can preserve the last heartbeat value.
        /// Implementations MUST ensure that stop blocks and does not return if the heartbeat is in the middle of updating.
        /// </remarks>
        void Stop();
    }
}
