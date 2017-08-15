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
    /// Configuration settings for sending messages
    /// </summary>
    public class QueueConfigurationSend
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueConfigurationSend" /> class.
        /// </summary>
        /// <param name="transportConfiguration">The transport configuration.</param>
        /// <param name="headerNames">The header names.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="timeConfiguration">The time configuration.</param>
        public QueueConfigurationSend(TransportConfigurationSend transportConfiguration, 
            IHeaders headerNames,
            IConfiguration configuration,
            BaseTimeConfiguration timeConfiguration)
        {
            Guard.NotNull(() => transportConfiguration, transportConfiguration);
            Guard.NotNull(() => headerNames, headerNames);
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => timeConfiguration, timeConfiguration);

            TransportConfiguration = transportConfiguration;
            HeaderNames = headerNames;
            AdditionalConfiguration = configuration;
            TimeConfiguration = timeConfiguration;
        }
        #endregion

        #region Public Props
        /// <summary>
        /// Gets or sets the transport configuration.
        /// </summary>
        /// <value>
        /// The transport configuration.
        /// </value>
        public TransportConfigurationSend TransportConfiguration { get; }

        /// <summary>
        /// Gets the header names.
        /// </summary>
        /// <value>
        /// The header names.
        /// </value>
        public IHeaders HeaderNames { get; }

        /// <summary>
        /// Gets the additional configuration settings.
        /// </summary>
        /// <value>
        /// The additional configuration.
        /// </value>
        /// <remarks>It's expected that extension methods will be used to access this in a type safe manner</remarks>
        public IConfiguration AdditionalConfiguration { get; }

        /// <summary>
        /// Gets the time configuration.
        /// </summary>
        /// <value>
        /// The time configuration.
        /// </value>
        public BaseTimeConfiguration TimeConfiguration { get; }
        #endregion
    }
}
