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
namespace DotNetWorkQueue.Configuration
{
    /// <inheritdoc cref="QueueConfigurationSend" />
    public class QueueProducerConfiguration : QueueConfigurationSend, IReadonly, ISetReadonly
    {
        private bool _isReadonly;
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueProducerConfiguration" /> class.
        /// </summary>
        /// <param name="transportConfiguration">The transport configuration.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="additionalConfiguration">The additional configuration.</param>
        /// <param name="timeConfiguration">The time configuration.</param>
        /// <param name="sendPolicies">The send policies.</param>
        public QueueProducerConfiguration(TransportConfigurationSend transportConfiguration,
            IHeaders headers,
            IConfiguration additionalConfiguration,
            BaseTimeConfiguration timeConfiguration,
            IPolicies sendPolicies)
            : base(transportConfiguration, headers, additionalConfiguration, timeConfiguration, sendPolicies)
        {

        }
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
    }
}
