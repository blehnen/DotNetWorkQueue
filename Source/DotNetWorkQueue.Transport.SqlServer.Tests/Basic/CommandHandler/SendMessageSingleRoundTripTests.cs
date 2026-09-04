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
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.CommandHandler
{
    /// <summary>
    /// The batch an ordinary send issues as a single round trip.
    /// </summary>
    /// <remarks>
    /// The shape matters for correctness, not just speed: the transaction now lives in the SQL
    /// rather than on the client, so the statements that make it atomic have to be there.
    /// </remarks>
    [TestClass]
    public class SendMessageSingleRoundTripTests
    {
        [TestMethod]
        public void The_Batch_Is_Atomic_And_Self_Contained()
        {
            var sql = Build(out _);

            //XACT_ABORT dooms the transaction on ordinary run-time errors, but it has no effect
            //on RAISERROR - a trigger raising one would otherwise reach the COMMIT and leave a
            //body row behind. The explicit rollback is what closes that.
            Assert.Contains("SET XACT_ABORT ON", sql);
            Assert.Contains("BEGIN TRANSACTION", sql);
            Assert.Contains("COMMIT TRANSACTION", sql);
            Assert.Contains("BEGIN TRY", sql);
            Assert.Contains("BEGIN CATCH", sql);
            Assert.Contains("IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION", sql);
            Assert.Contains("THROW", sql);

            //the identity never returns to the client mid-batch
            Assert.Contains("DECLARE @QueueID bigint", sql);
            Assert.Contains("SET @QueueID = SCOPE_IDENTITY()", sql);
            Assert.Contains("SELECT @QueueID", sql);
        }

        [TestMethod]
        public void It_Writes_Both_Rows()
        {
            var sql = Build(out _);

            Assert.Contains("Insert into queue", sql);
            Assert.Contains("Insert into meta", sql);
        }

        [TestMethod]
        public void QueueId_Is_A_Variable_Not_A_Parameter()
        {
            //the trap: the batch declares @QueueID, so a parameter of that name would collide with
            //the declaration and the send would fail outright
            var sql = Build(out var command);

            Assert.Contains("DECLARE @QueueID", sql);
            Assert.IsFalse(command.Parameters.Contains("@QueueID"),
                "@QueueID must be the batch's own variable, not a parameter");
            Assert.IsTrue(command.Parameters.Contains("@CorrelationID"),
                "the other meta parameters still have to be bound");
        }

        [TestMethod]
        public void The_Same_Shape_Is_Served_From_The_Cache()
        {
            var first = Build(out _);
            var second = Build(out _);

            Assert.AreEqual(first, second);
        }

        [TestMethod]
        public void A_Delayed_Message_Gets_Its_Own_Batch()
        {
            //a delay is written into the meta statement as a literal, so its batch differs per
            //message and must not come from the cache
            var none = Build(out _);
            var fiveSeconds = Build(out _, delay: TimeSpan.FromSeconds(5), delayedProcessing: true);
            var tenSeconds = Build(out _, delay: TimeSpan.FromSeconds(10), delayedProcessing: true);

            Assert.AreNotEqual(none, fiveSeconds);
            Assert.AreNotEqual(fiveSeconds, tenSeconds);
            Assert.Contains("5000", fiveSeconds);
            Assert.Contains("10000", tenSeconds);
        }

        [TestMethod]
        public void The_Status_Table_Insert_Joins_The_Same_Batch()
        {
            //the status insert used to be excluded on the belief that its @CorrelationID would
            //collide with the meta insert's. It does not - one SqlParameter serves every
            //occurrence of the name - so a status-table queue gets the single round trip too.
            var sql = Build(out var command, statusTable: true);

            Assert.Contains("Insert into queue", sql);
            Assert.Contains("Insert into meta", sql);
            Assert.Contains("Insert into status", sql);

            Assert.AreEqual(1, CountParameters(command, "@CorrelationID"),
                "@CorrelationID must be bound once and referenced twice, not bound twice");
            Assert.IsFalse(command.Parameters.Contains("@QueueID"),
                "@QueueID is still the batch's own variable");
        }

        [TestMethod]
        public void A_Status_Batch_Served_From_The_Cache_Still_Binds_Its_Parameters()
        {
            //the text can come from the cache, but the parameters never can - they carry this
            //message's values. Building the status insert before the cache is consulted is what
            //keeps that true.
            Build(out _, statusTable: true);
            var sql = Build(out var second, statusTable: true);

            Assert.Contains("Insert into status", sql);
            Assert.AreEqual(1, CountParameters(second, "@CorrelationID"));
        }

        [TestMethod]
        public void A_Status_Batch_Carrying_User_Columns_Is_Not_Cached()
        {
            //with the user's columns on the status table rather than the meta table, their names
            //are written into the status insert - so that text varies per message and caching it
            //would serve one message's columns to another
            var first = Build(out _, statusTable: true, userColumn: "OrderId");
            var second = Build(out _, statusTable: true, userColumn: "CustomerId");

            Assert.Contains("OrderId", first);
            Assert.Contains("CustomerId", second);
            Assert.AreNotEqual(first, second);
        }

        private static int CountParameters(SqlCommand command, string name)
        {
            var count = 0;
            foreach (SqlParameter parameter in command.Parameters)
            {
                if (string.Equals(parameter.ParameterName, name, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            return count;
        }

        private static string Build(out SqlCommand command, TimeSpan? delay = null,
            bool delayedProcessing = false, bool statusTable = false, string userColumn = null)
        {
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.QueueName.Returns("queue");
            tableNameHelper.MetaDataName.Returns("meta");
            tableNameHelper.StatusName.Returns("status");

            var options = new SqlServerMessageQueueTransportOptions
            {
                EnableDelayedProcessing = delayedProcessing,
                EnableStatusTable = statusTable
            };

            var data = new AdditionalMessageData
            {
                CorrelationId = new MessageCorrelationId<Guid>(Guid.NewGuid())
            };

            if (userColumn != null)
            {
                data.AdditionalMetaData.Add(new AdditionalMetaData<string>(userColumn, "a-value"));
            }

            command = new SqlCommand();
            SendMessage.BuildSingleRoundTripCommand(command, tableNameHelper, Substitute.For<IHeaders>(),
                data, Substitute.For<IMessage>(), options, delay, TimeSpan.Zero);
            return command.CommandText;
        }
    }
}
