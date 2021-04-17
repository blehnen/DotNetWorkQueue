using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using LiteDB;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests
{
    public static class Helpers
    {
        public static void Verify(QueueConnection queueConnection, QueueProducerConfiguration queueProducerConfiguration, long messageCount, ICreationScope scope)
        {
            new VerifyQueueData(queueConnection, queueProducerConfiguration.Options(), scope).Verify(messageCount, null);
        }

        public static void Verify(QueueConnection queueConnection, QueueProducerConfiguration queueProducerConfiguration, long messageCount, string route, ICreationScope scope)
        {
            new VerifyQueueData(queueConnection, queueProducerConfiguration.Options(), scope).Verify(messageCount, route);
        }

        public static void Verify(QueueConnection queueConnection, long messageCount, ICreationScope scope)
        {
            var connection = new LiteDbConnectionInformation(queueConnection);
            var helper = new TableNameHelper(connection);
            var connScope = scope.GetDisposable<LiteDbConnectionManager>();
            if (connScope == null)
            {
                using (var conn = new LiteDatabase(connection.ConnectionString))
                {
                    var col = conn.GetCollection<Schema.MetaDataTable>(helper.MetaDataName);
                    Assert.Equal(messageCount, col.Count());
                }
            }
            else
            {
                using (var conn = connScope.GetDatabase())
                {
                    var col = conn.Database.GetCollection<Schema.MetaDataTable>(helper.MetaDataName);
                    Assert.Equal(messageCount, col.Count());
                }
            }
        }

        public static void SetError(QueueConnection queueConnection, ICreationScope scope)
        {
            var connection = new LiteDbConnectionInformation(queueConnection);
            var helper = new TableNameHelper(connection);
            var connScope = scope.GetDisposable<LiteDbConnectionManager>();
            if (connScope == null)
            {
                using (var conn = new LiteDatabase(connection.ConnectionString))
                {
                    var col = conn.GetCollection<Schema.StatusTable>(helper.StatusName);
                    var results = col.Query()
                        .ToList();
                    foreach (var result in results)
                    {
                        result.Status = QueueStatuses.Error;
                        col.Update(result);
                    }
                }
            }
            else
            {
                using (var conn = connScope.GetDatabase())
                {
                    var col = conn.Database.GetCollection<Schema.StatusTable>(helper.StatusName);
                    var results = col.Query()
                        .ToList();
                    foreach (var result in results)
                    {
                        result.Status = QueueStatuses.Error;
                        col.Update(result);
                    }
                }
            }
        }

        public static void NoVerification(QueueConnection queueConnection, QueueProducerConfiguration queueProducerConfiguration, long messageCount, ICreationScope scope)
        {

        }

        public static AdditionalMessageData GenerateData(QueueProducerConfiguration configuration)
        {
            if (configuration.Options().EnableMessageExpiration ||
                configuration.Options().EnableDelayedProcessing)
            {
                var data = new AdditionalMessageData();

                if (configuration.Options().EnableMessageExpiration)
                    data.SetExpiration(TimeSpan.FromSeconds(1));

                if (configuration.Options().EnableDelayedProcessing)
                    data.SetDelay(TimeSpan.FromSeconds(5));

                return data;
            }

            return new AdditionalMessageData();
        }

        public static void SetOptions(LiteDbMessageQueueCreation oCreation, 
            bool enableDelayedProcessing,
            bool enableMessageExpiration,
            bool enableStatusTable,
            bool enableRoute = false)
        {
            oCreation.Options.EnableDelayedProcessing = enableDelayedProcessing;
            oCreation.Options.EnableMessageExpiration = enableMessageExpiration;
            oCreation.Options.EnableStatusTable = enableStatusTable;
            oCreation.Options.EnableRoute = enableRoute;
        }

        public static void VerifyQueueCount(QueueConnection connection, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            new VerifyQueueRecordCount(connection.Queue, connection.Connection, (LiteDbMessageQueueTransportOptions)arg3, arg4)
                .Verify(arg5, arg6, arg7);
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
