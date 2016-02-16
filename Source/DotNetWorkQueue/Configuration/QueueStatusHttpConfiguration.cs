// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.QueueStatus;

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// The configuration module for <see cref="QueueStatusHttp"/>
    /// </summary>
    public class QueueStatusHttpConfiguration
    {
        /// <summary>
        /// Gets or sets the listener address.
        /// </summary>
        /// <value>
        /// The listener address.
        /// </value>
        public Uri ListenerAddress { get; set; }

        /// <summary>
        /// Returns a <see cref="String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return ListenerAddress?.ToString() ?? base.ToString();
        }
    }
    /// <summary>
    /// Configuration extensions for setting the http queue status options
    /// </summary>
    public static class ConfigurationExtensionsForQueueStatusHttpConfiguration
    {
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public static QueueStatusHttpConfiguration Options(this IQueueStatus queueStatus)
        {
            Guard.NotNull(() => queueStatus, queueStatus);
            dynamic options;
            if (queueStatus.Configuration.TryGetSetting("QueueStatusHttpConfiguration", out options))
            {
                return options;
            }
            throw new DotNetWorkQueueException("Failed to obtain the options"); 
        }
    }
}
