using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    public static class Helpers
    {
        public static string DefaultRoute = "route1";

        public static void Verify(string queueName, string connectionString, QueueProducerConfiguration queueProducerConfiguration, long messageCount, string route, ICreationScope scope)
        {
            using (var verify = new VerifyQueueData(queueName, queueProducerConfiguration, connectionString))
            {
                verify.Verify(messageCount, 0, route);
            }
        }

        public static void Verify(string queueName, string connectionString, QueueProducerConfiguration queueProducerConfiguration, long messageCount, ICreationScope scope)
        {
            using (var verify = new VerifyQueueData(queueName, queueProducerConfiguration, connectionString))
            {
                verify.Verify(messageCount, 0, null);
            }
        }

        public static void Verify(string queueName, string connectionString, long messageCount, ICreationScope scope)
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

        public static void SetError(string queueName, string connectionString, ICreationScope scope)
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

        public static AdditionalMessageData GenerateRouteData(QueueProducerConfiguration configuration)
        {
            configuration.SetRoute(true);
            return GenerateData(configuration);
        }

        public static AdditionalMessageData GenerateExpiredDataWithRoute(QueueProducerConfiguration configuration)
        {
            configuration.SetMessageExpiration(true);
            configuration.SetRoute(true);
            return GenerateData(configuration);
        }

        public static AdditionalMessageData GenerateData(QueueProducerConfiguration configuration)
        {
            if (configuration.GetMessageExpiration().HasValue ||
                configuration.GetMessageDelay().HasValue ||
                configuration.GetMessageRoute().HasValue)
            {
                var data = new AdditionalMessageData();

                // ReSharper disable once PossibleInvalidOperationException
                if (configuration.GetMessageExpiration().HasValue && configuration.GetMessageExpiration().Value)
                    data.SetExpiration(TimeSpan.FromSeconds(1));

                // ReSharper disable once PossibleInvalidOperationException
                if (configuration.GetMessageDelay().HasValue && configuration.GetMessageDelay().Value)
                    data.SetDelay(TimeSpan.FromSeconds(5));

                // ReSharper disable once PossibleInvalidOperationException
                if (configuration.GetMessageRoute().HasValue && configuration.GetMessageRoute().Value)
                    data.Route = DefaultRoute;

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
            return queue.AdditionalConfiguration.TryGetSetting("RedisMessageExpirationEnabled", out dynamic value) ? value : null;
        }
        public static void SetMessageDelay(this QueueConfigurationSend queue, bool enabled)
        {
            queue.AdditionalConfiguration.SetSetting("RedisMessageDelayEnabled", enabled);
        }
        public static void SetRoute(this QueueConfigurationSend queue, bool enabled)
        {
            queue.AdditionalConfiguration.SetSetting("RedisRouteEnabled", enabled);
        }
        public static bool? GetMessageDelay(this QueueConfigurationSend queue)
        {
            return queue.AdditionalConfiguration.TryGetSetting("RedisMessageDelayEnabled", out dynamic value) ? value : null;
        }
        public static bool? GetMessageRoute(this QueueConfigurationSend queue)
        {
            return queue.AdditionalConfiguration.TryGetSetting("RedisRouteEnabled", out dynamic value) ? value : null;
        }
    }
}
