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
namespace DotNetWorkQueue
{
    /// <inheritdoc />
    public interface IWorker : IWorkerBase
    {
        /// <summary>
        /// Gets the idle status.
        /// </summary>
        /// <value>
        /// The idle status.
        /// </value>
        WorkerIdleStatus IdleStatus { get; }
    }

    /// <summary>
    /// Indicates the current status of the worker; idle or not idle
    /// </summary>
    public enum WorkerIdleStatus
    {
        /// <summary>
        /// The idle status has not been set
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The worker is idle
        /// </summary>
        Idle = 1,
        /// <summary>
        /// The worker is not idle
        /// </summary>
        NotIdle = 2
    }
}