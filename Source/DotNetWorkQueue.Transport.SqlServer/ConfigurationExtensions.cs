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
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
            return data.TryGetSetting("SqlServerMessageQueueDelay", out dynamic value) ? value : null;
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
            return data.TryGetSetting("SqlServerMessageQueueExpiration", out dynamic value) ? value : null;
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
            return data.TryGetSetting("SqlServerMessageQueuePriority", out dynamic value) ? value : (ushort)128;
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
            if (configuration.AdditionalConfiguration.TryGetSetting("SqlServerMessageQueueTransportOptions", out dynamic options))
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
            if (configuration.AdditionalConfiguration.TryGetSetting("SqlServerMessageQueueTransportOptions", out dynamic options))
            {
                return options;
            }
            throw new DotNetWorkQueueException("Failed to obtain the options");
        }
    }

    /// <summary>
    /// Extenstion method for setting or obtaining the schema to use for the queue
    /// </summary>
    public static class QueueConnectionExtensions
    {
        private const string SqlSchemaName = "SqlSchema";
        /// <summary>
        /// Sets the key "SqlSchema" equal to the schema value
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="schema">The schema.</param>
        public static void SetSchema(this IDictionary<string, string> settings, string schema)
        {
            if (!settings.ContainsKey(SqlSchemaName))
            {
                settings.Add(SqlSchemaName, schema);
                return;
            }
            settings[SqlSchemaName] = schema;
        }

        /// <summary>
        /// Gets the schema by returning the value in the "SqlSchema" key, or by return the default value of "dbo"
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns></returns>
        public static string GetSchema(this IReadOnlyDictionary<string, string> settings)
        {
            return settings.ContainsKey(SqlSchemaName) ? settings[SqlSchemaName] : "dbo";
        }
    }

    internal static class QueueQueueConsumerConfigurationExtensions
    {
        public static List<SqlParameter> GetUserParameters(this QueueConsumerConfiguration configuration)
        {
            if (configuration.AdditionalSettings.ContainsKey("userdequeueparams"))
            {
                return (List<SqlParameter>)configuration.AdditionalSettings["userdequeueparams"];
            }
            return null;
        }
        public static string GetUserClause(this QueueConsumerConfiguration configuration)
        {
            if (configuration.AdditionalSettings.ContainsKey("userdequeue"))
            {
                return (string)configuration.AdditionalSettings["userdequeue"];
            }
            return null;
        }
    }
}
