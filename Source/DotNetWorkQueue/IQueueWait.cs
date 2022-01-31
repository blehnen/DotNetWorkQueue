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
using System;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Delays querying the transport for a length of time. 
    /// </summary>
    public interface IQueueWait
    {
        /// <summary>
        /// Resets the wait time back to the start.
        /// </summary>
        void Reset();
        /// <summary>
        /// Waits until the next wait time has expired
        /// </summary>
        void Wait();
        /// <summary>
        /// Waits until the specified time span has been reached.
        /// </summary>
        /// <param name="waitTime">The how long the wait will last.</param>
        void Wait(Action<TimeSpan> waitTime);
    }
}
