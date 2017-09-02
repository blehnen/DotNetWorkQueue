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
    /// The configuration module for an RPC queue
    /// </summary>
    public class QueueConfigurationRpc
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueConfigurationRpc" /> class.
        /// </summary>
        /// <param name="transportConfigurationSend">The transport configuration send.</param>
        /// <param name="transportConfigurationReceive">The transport configuration receive.</param>
        /// <param name="headerNames">The header names.</param>
        public QueueConfigurationRpc(TransportConfigurationSend transportConfigurationSend, 
            TransportConfigurationReceive transportConfigurationReceive,
            IHeaders headerNames)
        {
            Guard.NotNull(() => transportConfigurationSend, transportConfigurationSend);
            Guard.NotNull(() => transportConfigurationReceive, transportConfigurationReceive);
            Guard.NotNull(() => headerNames, headerNames);

            TransportConfigurationSend = transportConfigurationSend;
            TransportConfigurationReceive = transportConfigurationReceive;
            HeaderNames = headerNames;
        }
        #endregion

        #region Public Props
        /// <summary>
        /// Gets or sets the transport configuration for sending messages
        /// </summary>
        /// <value>
        /// The transport configuration.
        /// </value>
        public TransportConfigurationSend TransportConfigurationSend { get;  }
        /// <summary>
        /// Gets or sets the transport configuration for receiving messages
        /// </summary>
        /// <value>
        /// The transport configuration.
        /// </value>
        public TransportConfigurationReceive TransportConfigurationReceive { get; }

        /// <summary>
        /// Gets the header names.
        /// </summary>
        /// <value>
        /// The header names.
        /// </value>
        public IHeaders HeaderNames { get; }
        #endregion
    }
}
