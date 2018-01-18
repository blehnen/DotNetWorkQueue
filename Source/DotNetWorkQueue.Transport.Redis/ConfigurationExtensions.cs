using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Redis.Basic;

namespace DotNetWorkQueue.Transport.Redis
{

    /// <summary>
    /// Extension methods for setting redis specific properties on the additional message data classes
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
            data.SetSetting("RedisQueueDelay", delay);
        }

        /// <summary>
        /// Gets the message delay.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static TimeSpan? GetDelay(this IAdditionalMessageData data)
        {
            return data.TryGetSetting("RedisQueueDelay", out dynamic value) ? value : null;
        }

        /// <summary>
        /// Sets the message expiration.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="expiration">The expiration.</param>
        public static void SetExpiration(this IAdditionalMessageData data, TimeSpan? expiration)
        {
            data.SetSetting("RedisQueueExpiration", expiration);
        }

        /// <summary>
        /// Gets the message expiration.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static TimeSpan? GetExpiration(this IAdditionalMessageData data)
        {
            return data.TryGetSetting("RedisQueueExpiration", out dynamic value) ? value : null;
        }
    }

    /// <summary>
    /// Configuration extensions for setting redis transport options
    /// </summary>
    public static class ConfigurationExtensionsForQueueConfigurationReceive
    {
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public static RedisQueueTransportOptions Options(this QueueConfigurationReceive configuration)
        {
            if (configuration.AdditionalConfiguration.TryGetSetting("RedisQueueTransportOptions", out dynamic options))
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
        public static RedisQueueTransportOptions Options(this QueueConfigurationSend configuration)
        {
            if (configuration.AdditionalConfiguration.TryGetSetting("RedisQueueTransportOptions", out dynamic options))
            {
                return options;
            }
            throw new DotNetWorkQueueException("Failed to obtain the options");
        }
    }
}
