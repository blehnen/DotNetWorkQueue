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
using DotNetWorkQueue.Exceptions;
namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// The hearbeat configuration settings
    /// <remarks>
    /// The queue can 'ping' a record that is being processed and keep it alive. This allows for automatic recovery of records in which the processor
    /// has died and records are stuck in a processing state.
    /// </remarks>
    /// </summary>
    public class HeartBeatConfiguration :  IHeartBeatConfiguration
    {
        private TimeSpan _monitorTime;
        private TimeSpan _time;
        private int _interval;
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
        /// <summary>
        /// Configuration settings for the heart beat thread pool
        /// </summary>
        /// <value>
        /// The thread pool configuration.
        /// </value>
        public IHeartBeatThreadPoolConfiguration ThreadPoolConfiguration { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="HeartBeatConfiguration" /> is enabled.
        /// </summary>
        /// <value>
        ///  <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled => _transportConfigurationReceive.HeartBeatSupported;

        /// <summary>
        /// Gets or sets the heart beat monitor time
        /// </summary>
        /// <remarks>This controls how often the queue checks for records to reset.</remarks>
        /// <value>
        /// The heart beat monitor time
        /// </value>
        public TimeSpan MonitorTime
        {
            get { return _monitorTime; }
            set
            {
                FailIfReadOnly();
                _monitorTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the heart beat time. See also <see cref="Interval"/>
        /// </summary>
        /// <remarks>This controls how long before a record is considered 'dead' because the heartbeat is out side of this window. The status will be reset, allowing re-processing</remarks>
        /// <value>
        /// The heart beat time
        /// </value>
        public TimeSpan Time
        {
            get { return _time; }
            set
            {
                FailIfReadOnly();
                _time = value;
            }
        }

        /// <summary>
        /// How often the heartbeat will be updated.
        /// </summary>
        /// <remarks>
        /// This is <see cref="Time"/> / <see cref="Interval"/>. If interval is 0, the check time will be 0, which means it is disabled.
        /// </remarks>
        /// <value>
        /// The heart beat check time.
        /// </value>
        /// <exception cref="DotNetWorkQueueException">Interval must be greater than 0</exception>
        public TimeSpan CheckTime => Interval > 0 ? TimeSpan.FromSeconds(Time.TotalSeconds / Interval) : TimeSpan.FromSeconds(0);

        /// <summary>
        /// Gets or sets the heart beat interval. See also <see cref="Time"/>
        /// </summary>
        /// <remarks>
        /// 
        /// How often the heart beat is updated. Should be at least 2; defaults to 4.
        /// 
        /// Say the heart beat time is 600 seconds. If the interval is 4, the hearbeat will be updated about every 150 seconds or so.
        /// 
        /// Higher values are safer, but increase writes to the transport. Lower values are risky - if the system is having trouble updating the heartbeat
        /// It's possible for multiple workers to get the same record. A value of 2 really means that the heartbeat may only make 1 attempt to be set before getting reset
        /// depending on timing.
        /// 
        /// </remarks>
        /// <value>
        /// The heart beat interval.
        /// </value>
        public int Interval
        {
            get { return _interval; }
            set
            {
                FailIfReadOnly();
                _interval = value;
            }
        }
        #endregion

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
            ThreadPoolConfiguration.SetReadOnly();
        }
    }
}
