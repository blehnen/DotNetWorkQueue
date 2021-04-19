// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Collections.Generic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Contains configuration settings for a queue that consumes messages
    /// </summary>
    public class QueueConsumerConfiguration : QueueConfigurationReceive, IReadonly, ISetReadonly
    {
        private bool _isReadonly;

        #region Constructor
        /// <summary>Initializes a new instance of the <see cref="QueueConsumerConfiguration"/> class.</summary>
        /// <param name="transportConfiguration">The transport configuration.</param>
        /// <param name="workerConfiguration">The worker configuration.</param>
        /// <param name="heartBeatConfiguration">The heart beat configuration.</param>
        /// <param name="messageExpirationConfiguration">The message expiration configuration.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="additionalConfiguration">The additional configuration.</param>
        /// <param name="messageErrorConfiguration">Configuration for if/when to delete messages in an error status</param>
        /// <param name="timeConfiguration">The time configuration.</param>
        public QueueConsumerConfiguration(TransportConfigurationReceive transportConfiguration, 
            IWorkerConfiguration workerConfiguration, 
            IHeartBeatConfiguration heartBeatConfiguration, 
            IMessageExpirationConfiguration messageExpirationConfiguration, 
            IHeaders headers,
            IConfiguration additionalConfiguration,
            IMessageErrorConfiguration messageErrorConfiguration,
            BaseTimeConfiguration timeConfiguration)
            : base(transportConfiguration, headers, additionalConfiguration, timeConfiguration)
        {
            Guard.NotNull(() => workerConfiguration, workerConfiguration);
            Guard.NotNull(() => heartBeatConfiguration, heartBeatConfiguration);
            Guard.NotNull(() => messageExpirationConfiguration, messageExpirationConfiguration);
            Guard.NotNull(() => messageErrorConfiguration, messageErrorConfiguration);

            Worker = workerConfiguration;
            HeartBeat = heartBeatConfiguration;
            MessageExpiration = messageExpirationConfiguration;
            MessageError = messageErrorConfiguration;
            Routes = new List<string>();
        }
        #endregion

        #region Configuration

        /// <summary>
        /// Worker configuration
        /// </summary>
        /// <value>
        /// Worker configuration
        /// </value>
        public IWorkerConfiguration Worker { get; }
        /// <summary>
        /// Heart Beat configuration
        /// </summary>
        /// <value>
        /// Heart Beat configuration
        /// </value>
        public IHeartBeatConfiguration HeartBeat { get; }

        /// <summary>
        /// Message expiration configuration
        /// </summary>
        /// <value>
        /// Message expiration configuration
        /// </value>
        public IMessageExpirationConfiguration MessageExpiration { get; }

        /// <summary>
        /// Message error configuration
        /// </summary>
        /// <remarks>Messages with an error status can be automaticly removed from the queue</remarks>
        public IMessageErrorConfiguration MessageError { get; }

        /// <summary>
        /// Gets or sets the routes.
        /// </summary>
        /// <value>
        /// The routes.
        /// </value>
        public List<string> Routes { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly
        {
            get => _isReadonly;
            protected set
            {
                _isReadonly = value;
                HeartBeat.SetReadOnly();
                MessageExpiration.SetReadOnly();
                Worker.SetReadOnly();
                TransportConfiguration.SetReadOnly();
            }
            
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
