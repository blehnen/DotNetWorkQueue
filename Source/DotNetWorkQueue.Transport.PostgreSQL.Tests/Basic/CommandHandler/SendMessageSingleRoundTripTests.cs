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
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.Shared.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.CommandHandler
{
    /// <summary>
    /// The statement an ordinary send issues as a single round trip.
    /// </summary>
    /// <remarks>
    /// The shape matters for correctness, not only speed. This is one <em>statement</em> built
    /// from data-modifying CTEs rather than a batch of several, and that is what makes it atomic
    /// without an explicit transaction - so the tests below check it stayed a single statement.
    /// </remarks>
    [TestClass]
    public class SendMessageSingleRoundTripTests
    {
        [TestMethod]
        public void It_Is_One_Statement_Not_A_Batch()
        {
            var sql = Build(out _);

            //a CTE chain, with the identity coming back from the body insert rather than lastval()
            Assert.Contains("WITH b AS", sql);
            Assert.Contains("RETURNING QueueID", sql);
            Assert.Contains("SELECT QueueID FROM b", sql);

            //a single statement is atomic on its own in PostgreSQL; a transaction here would mean
            //this had stopped being one
            Assert.DoesNotContain("BEGIN", sql);
            Assert.DoesNotContain("COMMIT", sql);
            Assert.DoesNotContain("lastval", sql);
        }

        [TestMethod]
        public void It_Writes_Both_Rows()
        {
            var sql = Build(out _);

            Assert.Contains("Insert into queue", sql);
            Assert.Contains("Insert into meta", sql);

            //the meta insert reads the identity out of the CTE rather than binding it
            Assert.Contains("b.QueueID", sql);
        }

        [TestMethod]
        public void QueueId_Is_A_Cte_Column_Not_A_Parameter()
        {
            var sql = Build(out var command);

            Assert.Contains("b.QueueID", sql);
            Assert.IsFalse(command.Parameters.Contains("@QueueID"),
                "@QueueID must come from the body insert's RETURNING, not from the client");
            Assert.IsTrue(command.Parameters.Contains("@CorrelationID"),
                "the other meta parameters still have to be bound");
        }

        [TestMethod]
        public void The_Status_Table_Insert_Joins_The_Same_Statement()
        {
            var sql = Build(out var command, statusTable: true);

            Assert.Contains("Insert into queue", sql);
            Assert.Contains("Insert into meta", sql);
            Assert.Contains("Insert into status", sql);
            Assert.Contains(", s AS (", sql);

            Assert.AreEqual(1, CountParameters(command, "@CorrelationID"),
                "@CorrelationID must be bound once and referenced twice, not bound twice");
            Assert.IsFalse(command.Parameters.Contains("@QueueID"));
        }

        [TestMethod]
        public void The_Same_Shape_Is_Served_From_The_Cache()
        {
            var first = Build(out _);
            var second = Build(out _);

            Assert.AreEqual(first, second);
        }

        [TestMethod]
        public void A_Statement_Served_From_The_Cache_Still_Binds_Its_Parameters()
        {
            //the text may come from the cache; the parameters never can - they carry this
            //message's values
            Build(out _, statusTable: true);
            var sql = Build(out var second, statusTable: true);

            Assert.Contains("Insert into status", sql);
            Assert.AreEqual(1, CountParameters(second, "@CorrelationID"));
        }

        [TestMethod]
        public void A_Delayed_Processing_Queue_Is_Never_Cached()
        {
            //PostgreSQL inlines the current time as a literal tick count whenever delayed
            //processing is on - even for a message carrying no delay - so every send produces a
            //different statement. Caching it would serve one message's timestamp to another.
            var first = Build(out _, delayedProcessing: true, currentTime: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var second = Build(out _, delayedProcessing: true, currentTime: new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc));

            Assert.AreNotEqual(first, second);
        }

        [TestMethod]
        public void A_Status_Statement_Carrying_User_Columns_Is_Not_Cached()
        {
            //with the user's columns on the status table their names are written into that insert,
            //so the text varies per message
            var first = Build(out _, statusTable: true, userColumn: "OrderId");
            var second = Build(out _, statusTable: true, userColumn: "CustomerId");

            Assert.Contains("OrderId", first);
            Assert.Contains("CustomerId", second);
            Assert.AreNotEqual(first, second);
        }

        private static int CountParameters(NpgsqlCommand command, string name)
        {
            var count = 0;
            foreach (NpgsqlParameter parameter in command.Parameters)
            {
                if (string.Equals(parameter.ParameterName, name.TrimStart('@'), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parameter.ParameterName, name, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            return count;
        }

        private static string Build(out NpgsqlCommand command, bool statusTable = false,
            bool delayedProcessing = false, string userColumn = null, DateTime? currentTime = null)
        {
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.QueueName.Returns("queue");
            tableNameHelper.MetaDataName.Returns("meta");
            tableNameHelper.StatusName.Returns("status");

            var options = new PostgreSqlMessageQueueTransportOptions
            {
                EnableStatusTable = statusTable,
                EnableDelayedProcessing = delayedProcessing
            };

            var data = new AdditionalMessageData
            {
                CorrelationId = new MessageCorrelationId<Guid>(Guid.NewGuid())
            };

            if (userColumn != null)
            {
                data.AdditionalMetaData.Add(new AdditionalMetaData<string>(userColumn, "a-value"));
            }

            command = new NpgsqlCommand();
            SendMessage.BuildSingleRoundTripCommand(command, tableNameHelper, Substitute.For<IHeaders>(),
                data, Substitute.For<IMessage>(), options, null, TimeSpan.Zero,
                currentTime ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            return command.CommandText;
        }
    }
}
