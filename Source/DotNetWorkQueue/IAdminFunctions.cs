﻿// ---------------------------------------------------------------------
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
    /// Functions for modifying or listing existing data in the queue
    /// </summary>
    public interface IAdminFunctions
    {
        /// <summary>
        /// Returns a count of items in the queue.
        /// </summary>
        /// <param name="status">A status to filter on, null if none</param>
        /// <returns></returns>
        long? Count(QueueStatusAdmin? status);
    }

    /// <summary>
    /// The possible status for items in the queue
    /// </summary>
    public enum QueueStatusAdmin : short
    {
        /// <summary>
        /// Waiting for processing
        /// </summary>
        /// <remarks>Already queue, but waiting for processing</remarks>
        Waiting = 0,
        /// <summary>
        /// Currently being processed
        /// </summary>
        /// <remarks>In the queue and in the middle of being processed</remarks>
        Processing = 1,
    }
}
