// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
    /// Configuration for message history tracking.
    /// </summary>
    public class HistoryConfiguration : IHistoryConfiguration
    {
        private bool _enabled;
        private int _retentionDays;
        private int _maxExceptionLength;
        private bool _storeBody;
        private bool _trackEnqueue;
        private bool _trackProcessing;
        private bool _trackComplete;
        private bool _trackError;
        private bool _trackDelete;
        private bool _trackExpire;
        private TimeSpan _monitorTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryConfiguration"/> class.
        /// </summary>
        public HistoryConfiguration()
        {
            Enabled = false;
            RetentionDays = 30;
            MaxExceptionLength = 4000;
            StoreBody = false;
            TrackEnqueue = true;
            TrackProcessing = true;
            TrackComplete = true;
            TrackError = true;
            TrackDelete = true;
            TrackExpire = true;
            MonitorTime = TimeSpan.FromDays(1);
        }

        /// <inheritdoc />
        public bool Enabled
        {
            get => _enabled;
            set
            {
                FailIfReadOnly();
                _enabled = value;
            }
        }

        /// <inheritdoc />
        public int RetentionDays
        {
            get => _retentionDays;
            set
            {
                FailIfReadOnly();
                _retentionDays = value;
            }
        }

        /// <inheritdoc />
        public int MaxExceptionLength
        {
            get => _maxExceptionLength;
            set
            {
                FailIfReadOnly();
                _maxExceptionLength = value;
            }
        }

        /// <inheritdoc />
        public bool StoreBody
        {
            get => _storeBody;
            set
            {
                FailIfReadOnly();
                _storeBody = value;
            }
        }

        /// <inheritdoc />
        public bool TrackEnqueue
        {
            get => _trackEnqueue;
            set
            {
                FailIfReadOnly();
                _trackEnqueue = value;
            }
        }

        /// <inheritdoc />
        public bool TrackProcessing
        {
            get => _trackProcessing;
            set
            {
                FailIfReadOnly();
                _trackProcessing = value;
            }
        }

        /// <inheritdoc />
        public bool TrackComplete
        {
            get => _trackComplete;
            set
            {
                FailIfReadOnly();
                _trackComplete = value;
            }
        }

        /// <inheritdoc />
        public bool TrackError
        {
            get => _trackError;
            set
            {
                FailIfReadOnly();
                _trackError = value;
            }
        }

        /// <inheritdoc />
        public bool TrackDelete
        {
            get => _trackDelete;
            set
            {
                FailIfReadOnly();
                _trackDelete = value;
            }
        }

        /// <inheritdoc />
        public bool TrackExpire
        {
            get => _trackExpire;
            set
            {
                FailIfReadOnly();
                _trackExpire = value;
            }
        }

        /// <inheritdoc />
        public TimeSpan MonitorTime
        {
            get => _monitorTime;
            set
            {
                FailIfReadOnly();
                _monitorTime = value;
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Throws an exception if the read only flag is true.
        /// </summary>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public void SetReadOnly()
        {
            IsReadOnly = true;
        }
    }
}
