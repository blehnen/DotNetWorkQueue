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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Producer
{
    /// <summary>
    /// An ordinary send is all-or-nothing, now that the four round trips are one statement.
    /// </summary>
    /// <remarks>
    /// The four-round-trip path holds an <see cref="NpgsqlTransaction"/> and rolls it back; the
    /// collapsed path holds none, because it is a single statement built from data-modifying CTEs
    /// and PostgreSQL runs a single statement atomically. That is a property of the statement
    /// staying a statement - if it were ever split into a batch, or an explicit transaction were
    /// introduced and mishandled, the body row could survive a failed meta insert and no consumer
    /// would ever see the message.
    /// <para>
    /// The failure has to happen at <em>run</em> time, which is why it is forced with a CHECK
    /// constraint. Dropping the meta table instead makes the statement fail to plan, so nothing
    /// executes at all - there is no body row to roll back and the test would pass no matter what
    /// the statement did. The SQL Server version of this test was written that way first and
    /// passed with its transaction removed entirely.
    /// </para>
    /// </remarks>
    [TestClass]
    public class SingleRoundTripIsAtomic
    {
        [TestMethod]
        public void A_Failed_Send_Leaves_No_Body_Row()
        {
            AssertFailedSendLeavesNothing(producer => producer.Send(new FakeMessage()));
        }

        /// <summary>
        /// The asynchronous handler has its own copy of the fast path, with its own connection and
        /// command execution, so it needs its own coverage rather than inheriting the synchronous
        /// one's.
        /// </summary>
        [TestMethod]
        public void A_Failed_Async_Send_Leaves_No_Body_Row()
        {
            AssertFailedSendLeavesNothing(producer =>
                producer.SendAsync(new FakeMessage()).GetAwaiter().GetResult());
        }

        private static void AssertFailedSendLeavesNothing(Func<IProducerQueue<FakeMessage>, IQueueOutputMessage> send)
        {
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
            var tableNameHelper = new TableNameHelper(new SqlConnectionInformation(queueConnection));

            using (var creation = new QueueCreationContainer<PostgreSqlMessageQueueInit>())
            {
                using var creator = creation.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                var created = creator.CreateQueue();
                Assert.IsTrue(created.Success, created.ErrorMessage);
            }

            try
            {
                //every meta insert now violates this, and it is a run-time failure - so the body
                //insert has already run by the time the statement fails
                Execute($"ALTER TABLE {tableNameHelper.MetaDataName} ADD CONSTRAINT chk_force_failure CHECK (QueueID < 0)");

                using (var container = new QueueContainer<PostgreSqlMessageQueueInit>())
                {
                    using var producer = container.CreateProducer<FakeMessage>(queueConnection);

                    var result = send(producer);
                    Assert.IsTrue(result.HasError, "the send should have failed on the constraint");

                    //Proves the test is not vacuous. A statement that failed to plan - which is
                    //what dropping the table would cause - never runs the body insert, so the
                    //assertion below would hold no matter what. A check-violation means execution
                    //reached the meta insert, so the body insert had already run.
                    Assert.Contains("chk_force_failure", DescribeFailure(result.SendingException),
                        "the send failed for some reason other than the forced constraint, so this "
                        + "test proves nothing about rollback");
                }

                Assert.AreEqual(0, CountRows(tableNameHelper.QueueName),
                    "the body row was committed although the meta insert failed - the send is not atomic");
            }
            finally
            {
                using var creation = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
                using var creator = creation.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                try { creator.RemoveQueue(); } catch (NpgsqlException) { /* best effort */ }
            }
        }

        /// <summary>The whole exception chain, since the provider error is wrapped by the library.</summary>
        private static string DescribeFailure(Exception exception)
        {
            var text = new System.Text.StringBuilder();
            for (var current = exception; current != null; current = current.InnerException)
            {
                text.AppendLine(current.Message);
            }
            return text.ToString();
        }

        private static void Execute(string sql)
        {
            using var connection = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static int CountRows(string table)
        {
            using var connection = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"select count(*) from {table}";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
