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

            //XACT_ABORT is what replaces the client-side rollback. Without it an error inside a
            //server-side transaction can leave it open.
            Assert.Contains("SET XACT_ABORT ON", sql);
            Assert.Contains("BEGIN TRANSACTION", sql);
            Assert.Contains("COMMIT TRANSACTION", sql);

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

        private static string Build(out SqlCommand command, TimeSpan? delay = null,
            bool delayedProcessing = false)
        {
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.QueueName.Returns("queue");
            tableNameHelper.MetaDataName.Returns("meta");

            var options = new SqlServerMessageQueueTransportOptions
            {
                EnableDelayedProcessing = delayedProcessing
            };

            var data = new AdditionalMessageData
            {
                CorrelationId = new MessageCorrelationId<Guid>(Guid.NewGuid())
            };

            command = new SqlCommand();
            SendMessage.BuildSingleRoundTripCommand(command, tableNameHelper, Substitute.For<IHeaders>(),
                data, Substitute.For<IMessage>(), options, delay, TimeSpan.Zero);
            return command.CommandText;
        }
    }
}
