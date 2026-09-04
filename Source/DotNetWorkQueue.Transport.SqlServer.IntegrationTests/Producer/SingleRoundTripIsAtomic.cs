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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Producer
{
    /// <summary>
    /// An ordinary send is all-or-nothing, now that its transaction lives in the SQL rather than
    /// on the client.
    /// </summary>
    /// <remarks>
    /// This is the property the single-round-trip path put at risk. The four-round-trip path holds
    /// a <c>SqlTransaction</c> and rolls it back in a <c>catch</c>; the batch has to do the same
    /// thing server-side. If it did not, the body row would be committed while the meta row was
    /// not - a message in the queue table that no consumer can ever see, and nothing would have
    /// failed loudly.
    /// <para>
    /// There are two cases, and they do not share a mechanism, which is why both are tested.
    /// <c>SET XACT_ABORT ON</c> covers an ordinary run-time error such as a constraint violation.
    /// It does <b>not</b> cover <c>RAISERROR</c>, so the batch also needs <c>TRY/CATCH</c> with an
    /// explicit <c>ROLLBACK</c>; with only the former, the trigger case below committed a body row
    /// while reporting the send as failed.
    /// </para>
    /// <para>
    /// The failure has to happen at <em>run</em> time, which is why it is forced with a CHECK
    /// constraint rather than by dropping the meta table. Dropping it makes the batch fail to
    /// compile, so nothing executes at all - there is no body row to roll back and the test passes
    /// no matter what the batch does. That version of this test passed with the transaction
    /// removed entirely, which is what makes it worth writing down.
    /// </para>
    /// <para>
    /// What this asserts is the outcome a caller cares about: a failed send leaves nothing behind.
    /// It does not isolate <c>XACT_ABORT</c> as the mechanism - a pooled connection also rolls back
    /// an open transaction when it is reset - and it is not intended to.
    /// </para>
    /// </remarks>
    [TestClass]
    public class SingleRoundTripIsAtomic
    {
        /// <summary>
        /// A constraint violation - an ordinary run-time error, which <c>XACT_ABORT</c> covers.
        /// </summary>
        [TestMethod]
        public void A_Failed_Send_Leaves_No_Body_Row()
        {
            AssertFailedSendLeavesNothing(meta =>
                $"ALTER TABLE {meta} ADD CONSTRAINT chk_force_failure CHECK (QueueID < 0)");
        }

        /// <summary>
        /// A trigger raising an error, which <c>XACT_ABORT</c> does <b>not</b> cover.
        /// </summary>
        /// <remarks>
        /// <c>SET XACT_ABORT ON</c> has no effect on errors raised by <c>RAISERROR</c>, so this
        /// case reaches the unconditional <c>COMMIT</c> and commits a body row whose send reported
        /// a failure. That is why the batch is wrapped in <c>TRY/CATCH</c> with an explicit
        /// rollback rather than relying on <c>XACT_ABORT</c> alone.
        /// </remarks>
        [TestMethod]
        public void A_Trigger_Raising_An_Error_Leaves_No_Body_Row()
        {
            AssertFailedSendLeavesNothing(meta =>
                $@"CREATE TRIGGER trg_force_raiserror ON {meta} AFTER INSERT AS
                   BEGIN
                       RAISERROR('forced failure from a trigger', 16, 1);
                   END");
        }

        /// <summary>
        /// Creates a queue, breaks the meta insert so that it fails <em>after</em> the body insert
        /// has run, sends, and asserts nothing was left behind.
        /// </summary>
        private static void AssertFailedSendLeavesNothing(Func<string, string> breakMetaInsert)
        {
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
            var tableNameHelper = new SqlServerTableNameHelper(new SqlConnectionInformation(queueConnection));

            using (var creation = new QueueCreationContainer<SqlServerMessageQueueInit>())
            {
                using var creator = creation.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
                var created = creator.CreateQueue();
                Assert.IsTrue(created.Success, created.ErrorMessage);
            }

            try
            {
                //the failure must happen at run time, so that the body insert has already executed
                //by the time the batch fails - see the remarks on this class
                Execute(breakMetaInsert(tableNameHelper.MetaDataName));

                using (var container = new QueueContainer<SqlServerMessageQueueInit>())
                {
                    using var producer = container.CreateProducer<FakeMessage>(queueConnection);

                    var result = producer.Send(new FakeMessage());
                    Assert.IsTrue(result.HasError, "the send should have failed");
                }

                //the point of the test: the body insert ran first and must have been rolled back
                Assert.AreEqual(0, CountRows(tableNameHelper.QueueName),
                    "the body row was committed although the meta insert failed - the batch is not atomic");
            }
            finally
            {
                using var creation = new QueueCreationContainer<SqlServerMessageQueueInit>();
                using var creator = creation.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
                try { creator.RemoveQueue(); } catch (SqlException) { /* best effort */ }
            }
        }

        private static void Execute(string sql)
        {
            using var connection = new SqlConnection(ConnectionInfo.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static int CountRows(string table)
        {
            using var connection = new SqlConnection(ConnectionInfo.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"select count(*) from {table}";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
