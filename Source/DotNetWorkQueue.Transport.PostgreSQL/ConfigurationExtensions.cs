// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL
{
    /// <summary>
    /// Extension methods for setting specific properties on the additional message data classes
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
            data.SetSetting("PostgreSQLMessageQueueDelay", delay);
        }
        /// <summary>
        /// Gets the message delay.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static TimeSpan? GetDelay(this IAdditionalMessageData data)
        {
            return data.TryGetSetting("PostgreSQLMessageQueueDelay", out dynamic value) ? value : null;
        }
        /// <summary>
        /// Sets the message expiration.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="expiration">The expiration.</param>
        public static void SetExpiration(this IAdditionalMessageData data, TimeSpan? expiration)
        {
            data.SetSetting("PostgreSQLMessageQueueExpiration", expiration);
        }
        /// <summary>
        /// Gets the message expiration.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static TimeSpan? GetExpiration(this IAdditionalMessageData data)
        {
            return data.TryGetSetting("PostgreSQLMessageQueueExpiration", out dynamic value) ? value : null;
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
            data.SetSetting("PostgreSQLMessageQueuePriority", priority);
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
            return data.TryGetSetting("PostgreSQLMessageQueuePriority", out dynamic value) ? value : (ushort)128;
        }
    }

    /// <summary>
    /// Configuration extensions for setting transport options
    /// </summary>
    public static class ConfigurationExtensionsForQueueConfigurationReceive
    {
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public static PostgreSqlMessageQueueTransportOptions Options(this QueueConfigurationReceive configuration)
        {
            if (configuration.AdditionalConfiguration.TryGetSetting("PostgreSQLMessageQueueTransportOptions", out dynamic options))
            {
                return options;
            }
            throw new DotNetWorkQueueException("Failed to obtain the options");
        }
    }


    /// <summary>
    /// Configuration extensions for setting transport options
    /// </summary>
    public static class ConfigurationExtensionsForQueueConfigurationSend
    {
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public static PostgreSqlMessageQueueTransportOptions Options(this QueueConfigurationSend configuration)
        {
            if (configuration.AdditionalConfiguration.TryGetSetting("PostgreSQLMessageQueueTransportOptions", out dynamic options))
            {
                return options;
            }
            throw new DotNetWorkQueueException("Failed to obtain the options");
        }
    }

    /// <summary>
    /// Extension methods for getting / adding user params for de-queue
    /// </summary>
    public static class QueueQueueConsumerConfigurationExtensions
    {
        private const string UserDequeueParamsFactoryKey = "userdequeueparamsfactory";
        private const string UserDequeueParamsKey = "userdequeueparams";
        private const string UserDequeueFactoryKey = "userdequeuefactory";
        /// <summary>
        /// Gets the user parameters for de-queue
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns></returns>
        /// <remarks>The factory method will always be returned if set, even if the non-factory method is also set</remarks>
        public static List<NpgsqlParameter> GetUserParameters(this QueueConsumerConfiguration configuration)
        {
            if (configuration.AdditionalSettings.TryGetValue(UserDequeueParamsFactoryKey, out var dequeueParamsFactory))
            {
                return ((Func<List<NpgsqlParameter>>)dequeueParamsFactory).Invoke();
            }
            if (configuration.AdditionalSettings.TryGetValue(UserDequeueParamsKey, out var dequeueParams))
            {
                return (List<NpgsqlParameter>)dequeueParams;
            }
            return null;
        }
        /// <summary>
        /// Gets the user where/and clause for de-queue
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns></returns>
        /// <remarks>The factory method will always be returned if set, even if the non-factory method is also set</remarks>
        public static string GetUserClause(this QueueConsumerConfiguration configuration)
        {
            if (configuration.AdditionalSettings.TryGetValue(UserDequeueFactoryKey, out var dequeueClauseFactory))
            {
                return ((Func<string>)dequeueClauseFactory).Invoke();
            }
            if (configuration.AdditionalSettings.TryGetValue("userdequeue", out var dequeueClause))
            {
                return (string)dequeueClause;
            }
            return null;
        }

        /// <summary>
        /// Sets the user parameters and clause via a factory method
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="whereClause">The where clause.</param>
        /// <remarks>The delegate will fire every time the queue begins to look for an item to de-queue</remarks>
        public static void SetUserParametersAndClause(this QueueConsumerConfiguration configuration, Func<List<NpgsqlParameter>> parameters, Func<string> whereClause)
        {
            if (configuration.AdditionalSettings.ContainsKey(UserDequeueParamsFactoryKey))
            {
                configuration.AdditionalSettings[UserDequeueParamsFactoryKey] = parameters;
            }
            else
            {
                configuration.AdditionalSettings.Add(UserDequeueParamsFactoryKey, parameters);
            }

            if (configuration.AdditionalSettings.ContainsKey(UserDequeueFactoryKey))
            {
                configuration.AdditionalSettings[UserDequeueFactoryKey] = whereClause;
            }
            else
            {
                configuration.AdditionalSettings.Add(UserDequeueFactoryKey, whereClause);
            }
        }

        /// <summary>
        /// Adds the user parameter. This same parameter will be used for every de-queue call
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="parameter">The parameter.</param>
        public static void AddUserParameter(this QueueConsumerConfiguration configuration, NpgsqlParameter parameter)
        {
            if (configuration.AdditionalSettings.TryGetValue(UserDequeueParamsKey, out var dequeueParams))
            {
                ((List<NpgsqlParameter>)dequeueParams).Add(parameter);
            }
            else
            {
                var data = new List<NpgsqlParameter> { parameter };
                configuration.AdditionalSettings.Add(UserDequeueParamsKey, data);
            }
        }

        /// <summary>
        /// Sets the user parameters. The same collection will be used for every de-queue call.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="parameters">The parameters.</param>
        public static void SetUserParameters(this QueueConsumerConfiguration configuration, List<NpgsqlParameter> parameters)
        {
            if (configuration.AdditionalSettings.ContainsKey(UserDequeueParamsKey))
            {
                configuration.AdditionalSettings[UserDequeueParamsKey] = parameters;
            }
            else
            {
                configuration.AdditionalSettings.Add(UserDequeueParamsKey, parameters);
            }
        }

        /// <summary>
        /// Sets the user where clause for custom de-queue 'AND' operations.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="whereClause">The where clause.</param>
        public static void SetUserWhereClause(this QueueConsumerConfiguration configuration, string whereClause)
        {
            if (configuration.AdditionalSettings.ContainsKey(UserDequeueParamsKey))
            {
                configuration.AdditionalSettings["userdequeue"] = whereClause;
            }
            else
            {
                configuration.AdditionalSettings.Add("userdequeue", whereClause);
            }
        }
    }
    /// <summary>
    /// Extension methods for the settings a schema upgrade needs.
    /// </summary>
    public static class QueueConnectionUpgradeExtensions
    {
        private const string UpgradeSourceTimeZoneName = "UpgradeSourceTimeZone";

        /// <summary>
        /// Declares the time zone that a queue created before 0.12.0 wrote its timestamps in.
        /// </summary>
        /// <param name="settings">The connection's additional settings.</param>
        /// <param name="timeZone">A PostgreSQL time zone name, for example "UTC" or "America/Chicago".</param>
        /// <remarks>
        /// Schema version 2 converts those columns from timestamp to timestamptz, and that conversion
        /// reads each naive value as being in the session's time zone. Before 0.12.0 the value stored
        /// was the local representation on the machine that wrote it, so the zone has to come from
        /// whoever knows what that machine was - nothing in the database records it.
        ///
        /// Set it to "UTC" if the application ran in UTC. Getting it wrong shifts every timestamp by
        /// the difference, which is the defect #311 fixed, so the upgrade refuses to guess (GitHub
        /// #374).
        /// </remarks>
        public static void SetUpgradeSourceTimeZone(this IDictionary<string, string> settings, string timeZone)
        {
            if (!settings.ContainsKey(UpgradeSourceTimeZoneName))
            {
                settings.Add(UpgradeSourceTimeZoneName, timeZone);
                return;
            }

            settings[UpgradeSourceTimeZoneName] = timeZone;
        }

        /// <summary>
        /// The declared source time zone, or null when none was set.
        /// </summary>
        /// <param name="settings">The connection's additional settings.</param>
        public static string GetUpgradeSourceTimeZone(this IReadOnlyDictionary<string, string> settings)
        {
            return settings.ContainsKey(UpgradeSourceTimeZoneName) ? settings[UpgradeSourceTimeZoneName] : null;
        }
    }
}
