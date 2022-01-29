using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Npgsql;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests
{
    public static class Helpers
    {
        public static void Verify(QueueConnection queueConnection, QueueProducerConfiguration queueProducerConfiguration, long messageCount, int orderId, ICreationScope scope)
        {
            new VerifyQueueData(queueConnection.Queue, queueProducerConfiguration.Options()).Verify(messageCount, null, orderId);
        }

        public static void Verify(QueueConnection queueConnection, QueueProducerConfiguration queueProducerConfiguration, long messageCount, string route, ICreationScope scope)
        {
            new VerifyQueueData(queueConnection.Queue, queueProducerConfiguration.Options()).Verify(messageCount, route);
        }

        public static void Verify(QueueConnection queueConnection, QueueProducerConfiguration queueProducerConfiguration, long messageCount, ICreationScope scope)
        {
            new VerifyQueueData(queueConnection.Queue, queueProducerConfiguration.Options()).Verify(messageCount, null);
        }

        public static void Verify(QueueConnection queueConnection, long messageCount, ICreationScope scope)
        {
            var connection = new SqlConnectionInformation(queueConnection);
            var helper = new TableNameHelper(connection);
            using (var conn = new NpgsqlConnection(queueConnection.Connection))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"select count(*) from {helper.StatusName}";
                    using (var reader = command.ExecuteReader())
                    {
                        Assert.True(reader.Read());
                        var records = reader.GetInt32(0);
                        Assert.Equal(messageCount, records);
                    }
                }
            }
        }

        public static void SetError(QueueConnection queueConnection, ICreationScope scope)
        {
            var connection = new SqlConnectionInformation(queueConnection);
            var helper = new TableNameHelper(connection);
            using (var conn = new NpgsqlConnection(queueConnection.Connection))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = $"update {helper.StatusName} set status = 2";
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void NoVerification(QueueConnection queueConnection, QueueProducerConfiguration queueProducerConfiguration, long messageCount, ICreationScope scope)
        {
            
        }

        public static AdditionalMessageData GenerateDataWithColumnValue(QueueProducerConfiguration configuration,
            int columnValue)
        {

            var data = new AdditionalMessageData();

            if (configuration.Options().EnableMessageExpiration)
                data.SetExpiration(TimeSpan.FromSeconds(1));

            if (configuration.Options().EnableDelayedProcessing)
                data.SetDelay(TimeSpan.FromSeconds(5));

            if (configuration.Options().EnablePriority)
                data.SetPriority(5);

            data.AdditionalMetaData.Add(new AdditionalMetaData<int>("OrderID", columnValue));
   
            return data;
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

        public static void VerifyQueueCount(QueueConnection queueConnection, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            new VerifyQueueRecordCount(queueConnection.Queue, (PostgreSqlMessageQueueTransportOptions)arg3).Verify(arg5, arg6, arg7);
        }

        public static void SetOptions(PostgreSqlMessageQueueCreation oCreation, bool enableDelayedProcessing,
            bool enableHeartBeat,
            bool enableHoldTransactionUntilMessageCommitted,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatus,
            bool enableStatusTable,
            bool additionalColumn,
            bool enableRoute = false,
            bool additionalColumnsOnMetaData = false)
        {
            oCreation.Options.EnableDelayedProcessing = enableDelayedProcessing;
            oCreation.Options.EnableHeartBeat = enableHeartBeat;
            oCreation.Options.EnableMessageExpiration = enableMessageExpiration;
            oCreation.Options.EnableHoldTransactionUntilMessageCommitted = enableHoldTransactionUntilMessageCommitted;
            oCreation.Options.EnablePriority = enablePriority;
            oCreation.Options.EnableStatus = enableStatus;
            oCreation.Options.EnableStatusTable = enableStatusTable;
            oCreation.Options.EnableRoute = enableRoute;
            oCreation.Options.AdditionalColumnsOnMetaData = additionalColumnsOnMetaData;

            if (additionalColumn)
            {
                oCreation.Options.AdditionalColumns.Add(new Column("OrderID", ColumnTypes.Integer, true));
                oCreation.Options.AdditionalConstraints.Add(new Constraint($"IX_OrderID{oCreation.ConnectionInfo.QueueName}", ConstraintType.Index, "OrderID"));
            }
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
