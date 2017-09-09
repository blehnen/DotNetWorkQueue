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

namespace DotNetWorkQueue.Configuration
{
    /// <inheritdoc />
    public class HeartBeatThreadPoolConfiguration: IHeartBeatThreadPoolConfiguration
    {
        private int _threadsMax;
        private int _queueMax;
        private TimeSpan _waitForThreadPoolToFinish;

        /// <inheritdoc />
        public int ThreadsMax
        {
            get => _threadsMax;
            set
            {
                FailIfReadOnly();
                _threadsMax = value;
            }
        }

        /// <inheritdoc />
        public int QueueMax
        {
            get => _queueMax;
            set
            {
                FailIfReadOnly();
                _queueMax = value;
            }
        }

        /// <inheritdoc />
        public TimeSpan WaitForThreadPoolToFinish
        {
            get => _waitForThreadPoolToFinish;
            set
            {
                FailIfReadOnly();
                _waitForThreadPoolToFinish = value;
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
        /// Throws an exception if the read-only flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <summary>
        /// Marks this instance as immutable
        /// </summary>
        public void SetReadOnly()
        {
            IsReadOnly = true;
        }
        #endregion
    }
}
