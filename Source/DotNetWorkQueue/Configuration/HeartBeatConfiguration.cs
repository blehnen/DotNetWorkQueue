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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// The heartbeat configuration settings
    /// <remarks>
    /// The queue can 'ping' a record that is being processed and keep it alive. This allows for automatic recovery of records in which the processor
    /// has died and records are stuck in a processing state.
    /// </remarks>
    /// </summary>
    public class HeartBeatConfiguration :  IHeartBeatConfiguration
    {
        private TimeSpan _monitorTime;
        private TimeSpan _time;
        private string _updateTime;
        private readonly TransportConfigurationReceive _transportConfigurationReceive;
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatConfiguration" /> class.
        /// </summary>
        /// <param name="transportConfiguration">The transport configuration.</param>
        /// <param name="threadPoolConfiguration">The thread pool configuration.</param>
        public HeartBeatConfiguration(TransportConfigurationReceive transportConfiguration, IHeartBeatThreadPoolConfiguration threadPoolConfiguration)
        {
            Guard.NotNull(() => transportConfiguration, transportConfiguration);
            Guard.NotNull(() => threadPoolConfiguration, threadPoolConfiguration);

            _transportConfigurationReceive = transportConfiguration;
            ThreadPoolConfiguration = threadPoolConfiguration;
            MonitorTime = TimeSpan.Zero;  
            Time = TimeSpan.Zero;
        }
        #endregion

        #region Configuration
        /// <inheritdoc />
        public IHeartBeatThreadPoolConfiguration ThreadPoolConfiguration { get; }

        /// <inheritdoc />
        public bool Enabled => _transportConfigurationReceive.HeartBeatSupported;

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
        public TimeSpan Time
        {
            get => _time;
            set
            {
                FailIfReadOnly();
                _time = value;
            }
        }

        /// <inheritdoc />
        public string UpdateTime
        {
            get => _updateTime;
            set
            {
                FailIfReadOnly();
                _updateTime = value;
            }
        }

        #endregion

        /// <inheritdoc />
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Throws an exception if the read-only flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public void SetReadOnly()
        {
            IsReadOnly = true;
            ThreadPoolConfiguration.SetReadOnly();
        }
    }
}
