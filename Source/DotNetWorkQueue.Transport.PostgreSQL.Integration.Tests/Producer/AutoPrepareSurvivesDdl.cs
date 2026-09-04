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
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Producer
{
    /// <summary>
    /// Whether Npgsql's automatic statement preparation survives the queue being dropped and
    /// recreated underneath a pooled connection.
    /// </summary>
    /// <remarks>
    /// This is the counterpart risk to turning auto-prepare on: prepared statements live on the
    /// physical connection, the pool hands that same connection back out, and dropping a table
    /// invalidates any prepared statement referencing it. This library creates and drops queues as
    /// a matter of course, so the question is not academic - a stale prepared statement would
    /// surface as a failed send against a queue that exists.
    /// <para>
    /// The queue name is reused deliberately: a fresh name would create fresh tables and prove
    /// nothing, since the prepared statements are keyed to the SQL text, which contains the table
    /// name.
    /// </para>
    /// </remarks>
    [TestClass]
    public class AutoPrepareSurvivesDdl
    {
        private const string AutoPrepare = "Max Auto Prepare=20;Auto Prepare Min Usages=2;";

        [TestMethod]
        public void A_Recreated_Queue_Still_Sends_With_Auto_Prepare_On()
        {
            var connectionString = WithAutoPrepare(ConnectionInfo.ConnectionString);
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, connectionString);

            //first generation: send enough times to get past Auto Prepare Min Usages, so the
            //statements really are prepared on the pooled connection
            CreateQueue(queueConnection);
            try
            {
                SendSeveral(queueConnection);
            }
            finally
            {
                RemoveQueue(queueConnection);
            }

            //second generation: same name, so the same SQL text, but different tables underneath.
            //Anything Npgsql prepared against the dropped tables is now stale.
            CreateQueue(queueConnection);
            try
            {
                SendSeveral(queueConnection);
            }
            finally
            {
                RemoveQueue(queueConnection);
            }
        }

        private static string WithAutoPrepare(string connectionString)
        {
            var separator = connectionString.TrimEnd().EndsWith(";", StringComparison.Ordinal) ? "" : ";";
            return connectionString + separator + AutoPrepare;
        }

        private static void CreateQueue(QueueConnection queueConnection)
        {
            using var creation = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            using var creator = creation.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
            var result = creator.CreateQueue();
            Assert.IsTrue(result.Success, result.ErrorMessage);
        }

        private static void RemoveQueue(QueueConnection queueConnection)
        {
            using var creation = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            using var creator = creation.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
            creator.RemoveQueue();
        }

        private static void SendSeveral(QueueConnection queueConnection)
        {
            using var container = new QueueContainer<PostgreSqlMessageQueueInit>();
            using var producer = container.CreateProducer<FakeMessage>(queueConnection);

            for (var i = 0; i < 10; i++)
            {
                var result = producer.Send(new FakeMessage());
                Assert.IsFalse(result.HasError,
                    result.SendingException?.ToString() ?? "send failed after the queue was recreated");
            }

            //and the batch path, which uses a different statement shape
            var batch = Enumerable.Range(0, 5).Select(_ => new FakeMessage()).ToList();
            foreach (var result in producer.Send(batch))
            {
                Assert.IsFalse(result.HasError,
                    result.SendingException?.ToString() ?? "batch send failed after the queue was recreated");
            }
        }
    }
}
