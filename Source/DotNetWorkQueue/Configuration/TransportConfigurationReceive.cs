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
using System.Collections.Generic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Transport configuration for receiving messages
    /// </summary>
    public class TransportConfigurationReceive : IReadonly, ISetReadonly
    {
        private bool _isReadonly;
        private bool _lockFeatures;
        private bool _heartBeatSupported;
        private bool _messageExpirationSupported;
        private bool _messageRollbackSupported;
        private IQueueDelay _queueDelayBehavior;
        private IQueueDelay _fatalExceptionDelayBehavior;
        private IRetryDelay _retryDelayBehavior;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportConfigurationReceive" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="queueDelayFactory">The queue delay factory.</param>
        /// <param name="retryDelayFactory">The retry delay factory.</param>
        public TransportConfigurationReceive(IConnectionInformation connectionInfo, 
            IQueueDelayFactory queueDelayFactory, 
            IRetryDelayFactory retryDelayFactory)
        {
            Guard.NotNull(() => connectionInfo, connectionInfo);
            Guard.NotNull(() => queueDelayFactory, queueDelayFactory);
            Guard.NotNull(() => retryDelayFactory, retryDelayFactory);
            ConnectionInfo = connectionInfo;

            QueueDelayBehavior = queueDelayFactory.Create(new List<TimeSpan>());
            RetryDelayBehavior = retryDelayFactory.Create();
            FatalExceptionDelayBehavior = queueDelayFactory.Create(new List<TimeSpan>());
        }
        #endregion

        #region Public Properties / Methods
        /// <summary>
        /// Gets the connection information.
        /// </summary>
        /// <value>
        /// The connection information.
        /// </value>
        public IConnectionInformation ConnectionInfo { get; }

        /// <summary>
        /// Gets or sets the queue delay behavior.
        /// </summary>
        /// <value>
        /// The queue delay behavior.
        /// </value>
        public IQueueDelay QueueDelayBehavior
        {
            get => _queueDelayBehavior;
            set
            {
                FailIfReadOnly();
                _queueDelayBehavior = value;
            }
        }

        /// <summary>
        /// How long to delay processing when a worker encounters a fatal exception 
        /// </summary>
        /// <remarks>Exceptions in user code do not count for this delay - this would be for errors such as the transport not responding</remarks>
        /// <value>
        /// The fatal exception delay behavior.
        /// </value>
        public IQueueDelay FatalExceptionDelayBehavior
        {
            get => _fatalExceptionDelayBehavior;
            set
            {
                FailIfReadOnly();
                _fatalExceptionDelayBehavior = value;
            }
        }

        /// <summary>
        /// Gets or sets the retry delay behavior.
        /// </summary>
        /// <value>
        /// The retry delay behavior.
        /// </value>
        public IRetryDelay RetryDelayBehavior
        {
            get => _retryDelayBehavior;
            set
            {
                FailIfReadOnly();
                _retryDelayBehavior = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this transport supports a heart beat
        /// </summary>
        /// <value>
        ///   <c>true</c> if [heart beat supported]; otherwise, <c>false</c>.
        /// </value>
        public bool HeartBeatSupported
        {
            get => _heartBeatSupported;
            set
            {
                FailIfLocked();
                _heartBeatSupported = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether message expiration is enabled
        /// </summary>
        /// <value>
        /// <c>true</c> if [message expiration supported]; otherwise, <c>false</c>.
        /// </value>
        public bool MessageExpirationSupported
        {
            get => _messageExpirationSupported;
            set
            {
                FailIfLocked();
                _messageExpirationSupported = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this transport can rollback a de-queue operation
        /// </summary>
        /// <value>
        /// <c>true</c> if [message rollback supported]; otherwise, <c>false</c>.
        /// </value>
        public bool MessageRollbackSupported
        {
            get => _messageRollbackSupported;
            set
            {
                FailIfLocked();
                _messageRollbackSupported = value;
            }
        }

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

                QueueDelayBehavior.SetReadOnly();
                FatalExceptionDelayBehavior.SetReadOnly();
                RetryDelayBehavior.SetReadOnly();
            }
        }

        /// <summary>
        /// Locks the features.
        /// </summary>
        public virtual void LockFeatures()
        {
            _lockFeatures = true;
        }

        /// <summary>
        /// Throws an exception if the read only flag is true.
        /// </summary>
        /// <exception cref="System.Data.ReadOnlyException"></exception>
        protected void FailIfReadOnly()
        {
            if (IsReadOnly) throw new InvalidOperationException();
        }

        /// <summary>
        /// Throws an exception if a setting is changed after the feature set is locked
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        protected void FailIfLocked()
        {
            if (_lockFeatures) throw new InvalidOperationException();
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
