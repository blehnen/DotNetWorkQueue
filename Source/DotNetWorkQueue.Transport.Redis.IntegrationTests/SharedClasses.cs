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
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    public static class Helpers
    {
        public static void Verify(string queueName, string connectionString, QueueProducerConfiguration queueProducerConfiguration, long messageCount, string route)
        {
            using (var verify = new VerifyQueueData(queueName, queueProducerConfiguration, connectionString))
            {
                verify.Verify(messageCount, 0, route);
            }
        }

        public static void Verify(string queueName, string connectionString, QueueProducerConfiguration queueProducerConfiguration, long messageCount)
        {
            using (var verify = new VerifyQueueData(queueName, queueProducerConfiguration, connectionString))
            {
                verify.Verify(messageCount, 0, null);
            }
        }

        public static void Verify(string queueName, string connectionString, long messageCount)
        {
            var connectionInfo = new BaseConnectionInformation(queueName, connectionString);
            var redisNames = new RedisNames(connectionInfo);
            using (var connection = new RedisConnection(connectionInfo))
            {
                var db = connection.Connection.GetDatabase();
                var records = db.HashLength(redisNames.Values);
                Assert.Equal(messageCount, records);
            }
        }

        public static void SetError(string queueName, string connectionString)
        {
            var connectionInfo = new BaseConnectionInformation(queueName, connectionString);
            var conn = new RedisConnection(connectionInfo);
            var redisNames = new RedisNames(connectionInfo);
            var db = conn.Connection.GetDatabase();
            var id = db.HashGet(redisNames.JobNames, "job1");
            db.HashSet(redisNames.Status, id,
               "2");
        }

        public static void NoVerification(string queueName, QueueProducerConfiguration queueProducerConfiguration, long messageCount)
        {

        }

        public static AdditionalMessageData GenerateExpiredData(QueueProducerConfiguration configuration)
        {
            configuration.SetMessageExpiration(true);
            return GenerateData(configuration);
        }

        public static AdditionalMessageData GenerateDelayData(QueueProducerConfiguration configuration)
        {
            configuration.SetMessageDelay(true);
            return GenerateData(configuration);
        }

        public static AdditionalMessageData GenerateDelayExpiredData(QueueProducerConfiguration configuration)
        {
            configuration.SetMessageDelay(true);
            configuration.SetMessageExpiration(true);
            return GenerateData(configuration);
        }

        public static AdditionalMessageData GenerateRouteData(QueueProducerConfiguration configuration, string route)
        {
            return new AdditionalMessageData {Route = route};
        }

        public static AdditionalMessageData GenerateData(QueueProducerConfiguration configuration)
        {
            if (configuration.GetMessageExpiration().HasValue ||
                configuration.GetMessageDelay().HasValue)
            {
                var data = new AdditionalMessageData();

                // ReSharper disable once PossibleInvalidOperationException
                if (configuration.GetMessageExpiration().HasValue && configuration.GetMessageExpiration().Value)
                    data.SetExpiration(TimeSpan.FromSeconds(1));

                // ReSharper disable once PossibleInvalidOperationException
                if (configuration.GetMessageDelay().HasValue && configuration.GetMessageDelay().Value)
                    data.SetDelay(TimeSpan.FromSeconds(5));

                return data;
            }

            return new AdditionalMessageData();
        }
    }
    /// <summary>
    /// Configuration extensions for setting redis transport options
    /// </summary>
    public static class ConfigurationExtensionsForQueueConfigurationSend
    {
        public static void SetMessageExpiration(this QueueConfigurationSend queue, bool enabled)
        {
            queue.AdditionalConfiguration.SetSetting("RedisMessageExpirationEnabled", enabled);
        }
        public static bool? GetMessageExpiration(this QueueConfigurationSend queue)
        {
            dynamic value;
            return queue.AdditionalConfiguration.TryGetSetting("RedisMessageExpirationEnabled", out value) ? value : null;
        }
        public static void SetMessageDelay(this QueueConfigurationSend queue, bool enabled)
        {
            queue.AdditionalConfiguration.SetSetting("RedisMessageDelayEnabled", enabled);
        }
        public static bool? GetMessageDelay(this QueueConfigurationSend queue)
        {
            dynamic value;
            return queue.AdditionalConfiguration.TryGetSetting("RedisMessageDelayEnabled", out value) ? value : null;
        }
    }
}
