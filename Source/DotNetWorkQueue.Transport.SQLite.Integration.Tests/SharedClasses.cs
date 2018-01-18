using System;
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests
{
    public static class Helpers
    {
        public static void Verify(string queueName, string connectionString, QueueProducerConfiguration queueProducerConfiguration, long messageCount, ICreationScope scope)
        {
            new VerifyQueueData(queueName, connectionString, queueProducerConfiguration.Options()).Verify(messageCount, null);
        }

        public static void Verify(string queueName, string connectionString, QueueProducerConfiguration queueProducerConfiguration, long messageCount, string route, ICreationScope scope)
        {
            new VerifyQueueData(queueName, connectionString, queueProducerConfiguration.Options()).Verify(messageCount, route);
        }

        public static void Verify(string queueName, string connectionString, long messageCount, ICreationScope scope)
        {
            var connection = new SqliteConnectionInformation(queueName, connectionString, new DbDataSource());
            var helper = new TableNameHelper(connection);
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select count(*) from {helper.MetaDataName}";
                    using (var reader = command.ExecuteReader())
                    {
                        Assert.True(reader.Read());
                        var records = reader.GetInt32(0);
                        Assert.Equal(messageCount, records);
                    }
                }
            }
        }

        public static void SetError(string queueName, string connectionString, ICreationScope scope)
        {
            var connection = new SqliteConnectionInformation(queueName, connectionString, new DbDataSource());
            var helper = new TableNameHelper(connection);
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"update {helper.StatusName} set status = 2";
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void NoVerification(string queueName, string connectionString, QueueProducerConfiguration queueProducerConfiguration, long messageCount, ICreationScope scope)
        {
            
        }

        public static AdditionalMessageData GenerateData(QueueProducerConfiguration configuration)
        {
            if (configuration.Options().EnableMessageExpiration ||
                configuration.Options().EnableDelayedProcessing ||
                configuration.Options().EnablePriority ||
                configuration.Options().AdditionalColumns.Count > 0)
            {
                var data = new AdditionalMessageData();

                if (configuration.Options().EnableMessageExpiration)
                    data.SetExpiration(TimeSpan.FromSeconds(1));

                if (configuration.Options().EnableDelayedProcessing)
                    data.SetDelay(TimeSpan.FromSeconds(5));

                if (configuration.Options().EnablePriority)
                    data.SetPriority(5);

                if (configuration.Options().AdditionalColumns.Count > 0)
                {
                    data.AdditionalMetaData.Add(new AdditionalMetaData<int>("OrderID", 123));
                }

                return data;
            }

            return null;
        }
    }

    public class IncrementWrapper
    {
        public IncrementWrapper()
        {
            ProcessedCount = 0;
        }
        public long ProcessedCount;
    }
}
