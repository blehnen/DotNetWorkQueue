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
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Configuration options for the redis queue
    /// </summary>
    public class RedisQueueTransportOptions : IReadonly, ISetReadonly
    {
        private TimeLocations _timeServer;
        private MessageIdLocations _messageIdLocations;
        private int _clearExpiredMessagesBatchLimit;
        private int _moveDelayedMessagesBatchLimit;
        private int _resetHeartBeatBatchLimit;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueTransportOptions"/> class.
        /// </summary>
        /// <param name="sntpTimeConfiguration">The SNTP time configuration.</param>
        /// <param name="delayedProcessingConfiguration">The delayed processing configuration.</param>
        public RedisQueueTransportOptions(SntpTimeConfiguration sntpTimeConfiguration, 
            DelayedProcessingConfiguration delayedProcessingConfiguration)
        {
            Guard.NotNull(() => sntpTimeConfiguration, sntpTimeConfiguration);
            Guard.NotNull(() => delayedProcessingConfiguration, delayedProcessingConfiguration);

            _clearExpiredMessagesBatchLimit = 50;
            _moveDelayedMessagesBatchLimit = 50;
            _resetHeartBeatBatchLimit = 50;
            DelayedProcessingConfiguration = delayedProcessingConfiguration;
            SntpTimeConfiguration = sntpTimeConfiguration;
        }

        /// <summary>
        /// Gets the SNTP time configuration.
        /// </summary>
        /// <value>
        /// The SNTP time configuration.
        /// </value>
        public SntpTimeConfiguration SntpTimeConfiguration
        {
            get; 
        }

        /// <summary>
        /// Gets or sets the time server implementation to use
        /// </summary>
        /// <value>
        /// The time server.
        /// </value>
        public TimeLocations TimeServer
        {
            get => _timeServer;
            set 
            { 
                FailIfReadOnly();
                _timeServer = value;
            }
        }

        /// <summary>
        /// Gets or sets the message identifier location to use
        /// </summary>
        /// <value>
        /// The message identifier location.
        /// </value>
        public MessageIdLocations MessageIdLocation
        {
            get => _messageIdLocations;
            set
            {
                FailIfReadOnly();
                _messageIdLocations = value;
            }
        }

        /// <summary>
        /// Gets or sets the clear expired messages batch limit.
        /// </summary>
        /// <value>
        /// The clear expired messages batch limit.
        /// </value>
        public int ClearExpiredMessagesBatchLimit
        {
            get => _clearExpiredMessagesBatchLimit;
            set
            {
                FailIfReadOnly();
                _clearExpiredMessagesBatchLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the move delayed messages batch limit.
        /// </summary>
        /// <value>
        /// The move delayed messages batch limit.
        /// </value>
        public int MoveDelayedMessagesBatchLimit
        {
            get => _moveDelayedMessagesBatchLimit;
            set
            {
                FailIfReadOnly();
                _moveDelayedMessagesBatchLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the reset heart beat batch limit.
        /// </summary>
        /// <value>
        /// The reset heart beat batch limit.
        /// </value>
        public int ResetHeartBeatBatchLimit
        {
            get => _resetHeartBeatBatchLimit;
            set
            {
                FailIfReadOnly();
                _resetHeartBeatBatchLimit = value;
            }
        }

        /// <summary>
        /// Gets the delayed processing configuration.
        /// </summary>
        /// <value>
        /// The delayed processing configuration.
        /// </value>
        public DelayedProcessingConfiguration DelayedProcessingConfiguration { get; }

        #region ReadOnly
        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Throws an exception if the read only flag is true.
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
            DelayedProcessingConfiguration.SetReadOnly();
            IsReadOnly = true;
        }
        #endregion
    }

    /// <summary>
    /// Delayed processing monitor configuration
    /// </summary>
    public class DelayedProcessingConfiguration : IMonitorTimespan, IReadonly, ISetReadonly
    {
        private TimeSpan _monitorTime;
        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedProcessingConfiguration"/> class.
        /// </summary>
        public DelayedProcessingConfiguration()
        {
            MonitorTime = TimeSpan.FromSeconds(1);
        }
        /// <summary>
        /// How often to look for delayed messages
        /// </summary>
        /// <value>
        /// The monitor time.
        /// </value>
        /// <remarks>This defaults to 1 second</remarks>
        public TimeSpan MonitorTime
        {
            get => _monitorTime;
            set
            {
                FailIfReadOnly();
                _monitorTime = value;
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
        /// Throws an exception if the read only flag is true.
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

    /// <summary>
    /// Where to obtain the current time from
    /// </summary>
    public enum TimeLocations
    {
        /// <summary>
        /// The local machine
        /// </summary>
        /// <remarks>This should only be used if all producer and consumer machine clocks are in sync</remarks>
        LocalMachine = 0,
        /// <summary>
        /// The redis server
        /// </summary>
        /// <remarks>This should only be used if all redis server clocks are in sync</remarks>
        RedisServer = 1,
        /// <summary>
        /// The SNTP server
        /// </summary>
        /// <remarks>This is the safest option, but is also the slowest</remarks>
        SntpServer = 2,
        /// <summary>
        /// A custom time client will be used.
        /// </summary>
        /// <remarks>Inject an implementation of <see cref="IUnixTime"/> into the container. The implementation may inherit from <see cref="BaseUnixTime"/> </remarks>
        Custom = 999
    }

    /// <summary>
    /// Where to obtain a new message Id
    /// </summary>
    public enum MessageIdLocations
    {
        /// <summary>
        /// A redis Incr command will be used to generate an id
        /// </summary>
        RedisIncr = 0,
        /// <summary>
        /// A <see cref="Guid.NewGuid()"/> call will be used to generate a new id
        /// </summary>
        Uuid = 1,
        /// <summary>
        /// A custom message id provider will be used.
        /// </summary>
        /// <remarks>Inject an implementation of <see cref="IGetMessageId"/> into the container</remarks>
        Custom = 999
    }
}
