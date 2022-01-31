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
using System.Threading;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Notifies user code of heart beat status updates
    /// </summary>
    public class WorkerHeartBeatNotification : IWorkerHeartBeatNotification
    {
        private readonly object _locker = new object();
        private long _errorCount;

        private IHeartBeatStatus _status;
        private CancellationToken _exceptionHasOccured;
        private Exception _error;

        /// <summary>
        /// Sets the error.
        /// </summary>
        /// <param name="error">The error.</param>
        public void SetError(Exception error)
        {
            lock (_locker)
            {
                _error = error;
            }
            Interlocked.Increment(ref _errorCount);
        }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public IHeartBeatStatus Status
        {
            get
            {
                lock (_locker)
                {
                    return _status;
                }
            }
            set
            {
                lock (_locker)
                {
                    _status = value;
                }
            }
        }

        /// <summary>
        /// This token will be tripped if an exception occurring trying to update the heartbeat.
        /// </summary>
        /// <value>
        /// The exception has occurred.
        /// </value>
        public CancellationToken ExceptionHasOccured
        {
            get
            {
                lock (_locker)
                {
                    return _exceptionHasOccured;
                }
            }
            set
            {
                lock (_locker)
                {
                    _exceptionHasOccured = value;
                }
            }
        }

        /// <summary>
        /// Gets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error
        {
            get
            {
                lock (_locker)
                {
                    return _error;
                }
            }
        }
        /// <summary>
        /// Gets the error count.
        /// </summary>
        /// <value>
        /// The error count.
        /// </value>
        public long ErrorCount => Interlocked.Read(ref _errorCount);
    }
}
