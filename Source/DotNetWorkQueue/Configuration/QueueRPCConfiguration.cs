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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    ///  RPC queue configuration class
    /// </summary>
    public class QueueRpcConfiguration : QueueConfigurationRpc, IReadonly, ISetReadonly
    {
        private bool _isReadonly;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueRpcConfiguration" /> class.
        /// </summary>
        /// <param name="transportConfigurationReceive">The transport configuration receive.</param>
        /// <param name="transportConfigurationSend">The transport configuration send.</param>
        /// <param name="messageExpirationConfiguration">The message expiration configuration.</param>
        /// <param name="headers">The headers.</param>
        public QueueRpcConfiguration(TransportConfigurationReceive transportConfigurationReceive, 
            TransportConfigurationSend transportConfigurationSend, 
            IMessageExpirationConfiguration messageExpirationConfiguration, 
            IHeaders headers)
            : base(transportConfigurationSend, transportConfigurationReceive, headers)
        {
            Guard.NotNull(() => messageExpirationConfiguration, messageExpirationConfiguration);
            MessageExpiration = messageExpirationConfiguration;
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Message expiration configuration
        /// </summary>
        /// <value>
        /// Message expiration configuration
        /// </value>
        public IMessageExpirationConfiguration MessageExpiration { get; }

        #endregion

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
                TransportConfigurationReceive.SetReadOnly();
                TransportConfigurationSend.SetReadOnly();
                MessageExpiration.SetReadOnly();
            }
        }

        /// <summary>
        /// Marks this instance as immutable
        /// </summary>
        public void SetReadOnly()
        {
            IsReadOnly = true;
        }
    }
}
