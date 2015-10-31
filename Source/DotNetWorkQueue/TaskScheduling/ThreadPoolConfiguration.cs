// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
namespace DotNetWorkQueue.TaskScheduling
{
    /// <summary>
    /// Configuration class for a thread pool
    /// </summary>
    public class ThreadPoolConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadPoolConfiguration"/> class.
        /// </summary>
        public ThreadPoolConfiguration()
        {
            IdleTimeout = TimeSpan.FromSeconds(5);
            MaxWorkerThreads = 1;
            MinWorkerThreads = 0;
            WaitForTheadPoolToFinish = TimeSpan.FromSeconds(5);
        }
        /// <summary>
        /// Gets or sets the idle timeout.
        /// </summary>
        /// <value>
        /// The idle timeout.
        /// </value>
        public TimeSpan IdleTimeout { get; set; }
        /// <summary>
        /// Gets or sets the maximum worker threads.
        /// </summary>
        /// <value>
        /// The maximum worker threads.
        /// </value>
        public int MaxWorkerThreads { get; set; }
        /// <summary>
        /// Gets or sets the minimum worker threads.
        /// </summary>
        /// <value>
        /// The minimum worker threads.
        /// </value>
        public int MinWorkerThreads { get; set; }
        /// <summary>
        /// How long to wait for thread pool threads to exit when shutting down
        /// </summary>
        public TimeSpan WaitForTheadPoolToFinish { get; set; }
    }
}
