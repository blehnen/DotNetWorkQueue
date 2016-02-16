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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.SqlServer.Basic;
namespace DotNetWorkQueue.Transport.SqlServer
{
    /// <summary>
    /// Extension methods for setting SQL server specific properties on the additional message data classes
    /// </summary>
    public static class ConfigurationExtensionsForIAdditionalMessageData
    {
        /// <summary>
        /// Sets the message delay.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="delay">The delay.</param>
        public static void SetDelay(this IAdditionalMessageData data, TimeSpan? delay)
        {
            data.SetSetting("SqlServerMessageQueueDelay", delay);
        }
        /// <summary>
        /// Gets the message delay.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static TimeSpan? GetDelay(this IAdditionalMessageData data)
        {
            dynamic value;
            return data.TryGetSetting("SqlServerMessageQueueDelay", out value) ? value : null;
        }
        /// <summary>
        /// Sets the message expiration.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="expiration">The expiration.</param>
        public static void SetExpiration(this IAdditionalMessageData data, TimeSpan? expiration)
        {
            data.SetSetting("SqlServerMessageQueueExpiration", expiration);
        }
        /// <summary>
        /// Gets the message expiration.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static TimeSpan? GetExpiration(this IAdditionalMessageData data)
        {
            dynamic value;
            return data.TryGetSetting("SqlServerMessageQueueExpiration", out value) ? value : null;
        }
        /// <summary>
        /// Sets the priority.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="priority">The priority.</param>
        /// <remarks>
        /// Defaults to 128. Min value is 0, max value is 255.
        /// 0 = highest priority
        /// 255 = lowest priority
        /// </remarks>
        public static void SetPriority(this IAdditionalMessageData data, ushort? priority)
        {
            data.SetSetting("SqlServerMessageQueuePriority", priority);
        }
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <remarks>
        /// Defaults to 128. Min value is 0, max value is 255.
        /// 0 = highest priority
        /// 255 = lowest priority
        /// </remarks>
        public static ushort? GetPriority(this IAdditionalMessageData data)
        {
            dynamic value;
            return data.TryGetSetting("SqlServerMessageQueuePriority", out value) ? value : 128;
        }
    }

    /// <summary>
    /// Configuration extensions for setting SQL server transport options
    /// </summary>
    public static class ConfigurationExtensionsForQueueConfigurationReceive
    {
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public static SqlServerMessageQueueTransportOptions Options(this QueueConfigurationReceive configuration)
        {
            dynamic options;
            if (configuration.AdditionalConfiguration.TryGetSetting("SqlServerMessageQueueTransportOptions", out options))
            {
                return options;
            }
            throw new DotNetWorkQueueException("Failed to obtain the options");
        }
    }


    /// <summary>
    /// Configuration extensions for setting SQL server transport options
    /// </summary>
    public static class ConfigurationExtensionsForQueueConfigurationSend
    {
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public static SqlServerMessageQueueTransportOptions Options(this QueueConfigurationSend configuration)
        {
            dynamic options;
            if (configuration.AdditionalConfiguration.TryGetSetting("SqlServerMessageQueueTransportOptions", out options))
            {
                return options;
            }
            throw new DotNetWorkQueueException("Failed to obtain the options");
        }
    }
}
