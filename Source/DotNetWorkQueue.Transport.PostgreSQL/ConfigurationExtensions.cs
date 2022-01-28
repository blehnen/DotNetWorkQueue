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
        /// <summary>
        /// Gets the user parameters for de-queue
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns></returns>
        /// <remarks>The factory method will always be returned if set, even if the non-factory method is also set</remarks>
        public static List<NpgsqlParameter> GetUserParameters(this QueueConsumerConfiguration configuration)
        {
            if (configuration.AdditionalSettings.ContainsKey("userdequeueparamsfactory"))
            {
                return ((Func<List<NpgsqlParameter>>)configuration.AdditionalSettings["userdequeueparamsfactory"]).Invoke();
            }
            if (configuration.AdditionalSettings.ContainsKey("userdequeueparams"))
            {
                return (List<NpgsqlParameter>)configuration.AdditionalSettings["userdequeueparams"];
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
            if (configuration.AdditionalSettings.ContainsKey("userdequeuefactory"))
            {
                return ((Func<string>)configuration.AdditionalSettings["userdequeuefactory"]).Invoke();
            }
            if (configuration.AdditionalSettings.ContainsKey("userdequeue"))
            {
                return (string)configuration.AdditionalSettings["userdequeue"];
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
            if (configuration.AdditionalSettings.ContainsKey("userdequeueparamsfactory"))
            {
                configuration.AdditionalSettings["userdequeueparamsfactory"] = parameters;
            }
            else
            {
                configuration.AdditionalSettings.Add("userdequeueparamsfactory", parameters);
            }

            if (configuration.AdditionalSettings.ContainsKey("userdequeuefactory"))
            {
                configuration.AdditionalSettings["userdequeuefactory"] = whereClause;
            }
            else
            {
                configuration.AdditionalSettings.Add("userdequeuefactory", whereClause);
            }
        }

        /// <summary>
        /// Adds the user parameter. This same parameter will be used for every de-queue call
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="parameter">The parameter.</param>
        public static void AddUserParameter(this QueueConsumerConfiguration configuration, NpgsqlParameter parameter)
        {
            if (configuration.AdditionalSettings.ContainsKey("userdequeueparams"))
            {
                ((List<NpgsqlParameter>)configuration.AdditionalSettings["userdequeueparams"]).Add(parameter);
            }
            else
            {
                var data = new List<NpgsqlParameter> { parameter };
                configuration.AdditionalSettings.Add("userdequeueparams", data);
            }
        }

        /// <summary>
        /// Sets the user parameters. The same collection will be used for every de-queue call.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="parameters">The parameters.</param>
        public static void SetUserParameters(this QueueConsumerConfiguration configuration, List<NpgsqlParameter> parameters)
        {
            if (configuration.AdditionalSettings.ContainsKey("userdequeueparams"))
            {
                configuration.AdditionalSettings["userdequeueparams"] = parameters;
            }
            else
            {
                configuration.AdditionalSettings.Add("userdequeueparams", parameters);
            }
        }

        /// <summary>
        /// Sets the user where clause for custom de-queue 'AND' operations.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="whereClause">The where clause.</param>
        public static void SetUserWhereClause(this QueueConsumerConfiguration configuration, string whereClause)
        {
            if (configuration.AdditionalSettings.ContainsKey("userdequeueparams"))
            {
                configuration.AdditionalSettings["userdequeue"] = whereClause;
            }
            else
            {
                configuration.AdditionalSettings.Add("userdequeue", whereClause);
            }
        }
    }
}
