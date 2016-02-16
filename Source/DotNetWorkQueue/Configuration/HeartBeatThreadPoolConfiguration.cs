// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Heart beat thread pool configuration module
    /// </summary>
    public class HeartBeatThreadPoolConfiguration: IHeartBeatThreadPoolConfiguration
    {
        private int _threadsMax;
        private int _threadsMin;
        private TimeSpan _threadIdleTimeout;

        /// <summary>
        /// How many threads will be used to update the heartbeats.
        /// </summary>
        /// <remarks>
        /// The thread pool is used to notify the worker that a heart beat needs updating. However, a dedicated thread pool is used for
        /// the actual updating, to reduce how long we require the usage of threads from the built in thread pool.
        /// </remarks>
        /// <value>
        /// The max heart beat threads.
        /// </value>
        public int ThreadsMax
        {
            get { return _threadsMax; }
            set
            {
                FailIfReadOnly();
                _threadsMax = value;
            }
        }

        /// <summary>
        /// How long before a heart beat worker thread idle time out due to inactivity.
        /// </summary>
        /// <remarks>This only applies if the concurrent thread count is greater than the min</remarks>
        /// <value>
        /// The heart beat thread idle timeout.
        /// </value>
        public TimeSpan ThreadIdleTimeout
        {
            get { return _threadIdleTimeout; }
            set
            {
                FailIfReadOnly();
                _threadIdleTimeout = value;
            }
        }

        /// <summary>
        /// The minimum amount of threads in the thread pool for updating the heartbeat
        /// </summary>
        /// <remarks>
        /// The thread pool is used to notify the worker that a heart beat needs updating. However, a dedicated thread pool is used for
        /// the actual updating, to reduce how long we require the usage of threads from the built in thread pool.
        /// </remarks>
        /// <value>
        /// The min heart beat threads.
        /// </value>
        public int ThreadsMin
        {
            get { return _threadsMin; }
            set
            {
                FailIfReadOnly();
                _threadsMin = value;
            }
        }

        #region ReadOnly
        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Throws an exception if the readonly flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <summary>
        /// Marks this instance as imutable
        /// </summary>
        public void SetReadOnly()
        {
            IsReadOnly = true;
        }
        #endregion
    }
}
