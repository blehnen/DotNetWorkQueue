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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Message expiration setting.
    /// <remarks>Messages can be set to expire. Used by the RPC queue, but can also be enabled for any queue.</remarks>
    /// </summary>
    public class MessageExpirationConfiguration : IMessageExpirationConfiguration
    {
        private TimeSpan _monitorTime;
        private bool _enabled;
        private readonly TransportConfigurationReceive _transportConfigurationReceive;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageExpirationConfiguration" /> class.
        /// </summary>
        /// <param name="transportConfiguration">The transport configuration.</param>
        public MessageExpirationConfiguration(TransportConfigurationReceive transportConfiguration)
        {
            Guard.NotNull(() => transportConfiguration, transportConfiguration);
            _transportConfigurationReceive = transportConfiguration;
        }
        #endregion

        #region Configuration
        /// <summary>
        /// If true, message expiration is enabled. Generally used by RPC queues, but can be used by any queue.
        /// </summary>
        /// <value>
        /// <c>true</c> if [message expiration enabled]; otherwise, <c>false</c>.
        /// </value>
        public bool Supported => _transportConfigurationReceive.MessageExpirationSupported;

        /// <summary>
        /// Returns the timespan for a monitor process
        /// </summary>
        /// <value>
        /// The monitor time span.
        /// </value>
        public TimeSpan MonitorTime
        {
            get => _monitorTime;
            set
            {
                FailIfReadOnly();
                _monitorTime = value;
            }
        }

        /// <summary>
        /// If true, the queue will check for and deleted expired messages. See <see cref="MonitorTime"/> as well.
        /// </summary>
        /// <remarks>
        /// The transport must support expiration <see cref="Supported"/> for this setting to have any effect. 
        /// If the queue supports message expiration and this setting is false, it's up to some other process to remove expired messages.
        /// Some transports can handle removing the expired messages without the queue needing to do anything.
        /// </remarks>
        /// <value>
        /// <c>true</c> if [clear expired messages enabled]; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled
        {
            get => _enabled && Supported;
            set
            {
                FailIfReadOnly();
                _enabled = value;
            }
        }
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
}
