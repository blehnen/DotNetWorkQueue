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
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.Basic
{
    /// <summary>
    /// Counting errors on LiteDb.
    ///
    /// LiteDb does the read and the write itself rather than handing a statement to a server, so the
    /// behaviour the relational transports get from their SQL has to be written out here - and it can
    /// drift from them without anything noticing. These are the same two properties their tests assert:
    /// replaying a total does not count a second failure, and a stale total does not lower the count
    /// (GitHub #350).
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class SetErrorCountTests
    {
        private const string ExceptionType = "System.Exception";

        [TestMethod]
        public void TheCountIsTheTotalWritten_AndNeverGoesBackwards()
        {
            using var connectionInfo = new IntegrationConnectionInfo(
                IntegrationConnectionInfo.ConnectionTypes.Direct);
            var queueName = GenerateQueueName.Create();
            var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);

            using (var creation = new QueueCreationContainer<LiteDbMessageQueueInit>())
            {
                using var creator = creation.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
                var created = creator.CreateQueue();
                Assert.IsTrue(created.Success, created.ErrorMessage);

                using (var container = new QueueContainer<LiteDbMessageQueueInit>())
                using (var admin = container.CreateAdminContainer(queueConnection))
                {
                    var handler = admin.GetInstance<ICommandHandler<SetErrorCountCommand<int>>>();
                    //read through the library rather than opening a second LiteDatabase on the same
                    //file - the connection here is exclusive, so a reader of our own does not see these
                    //writes and the test would be measuring its own plumbing
                    var count = admin.GetInstance<IQueryHandler<GetErrorRetryCountQuery<int>, int>>();
                    int Stored() => count.Handle(new GetErrorRetryCountQuery<int>(ExceptionType, 1));

                    //a first failure, then a second - the caller reads the count before each write
                    handler.Handle(new SetErrorCountCommand<int>(ExceptionType, 1, 1));
                    Assert.AreEqual(1, Stored());

                    handler.Handle(new SetErrorCountCommand<int>(ExceptionType, 1, 2));
                    Assert.AreEqual(2, Stored());

                    //the write is wrapped in a retry policy, so a fault raised after it had already
                    //been applied replays it - which must not count the same failure twice
                    handler.Handle(new SetErrorCountCommand<int>(ExceptionType, 1, 2));
                    Assert.AreEqual(2, Stored(),
                        "replaying the same total counted a second failure");

                    //and a total read before another worker advanced the row must not lower it
                    handler.Handle(new SetErrorCountCommand<int>(ExceptionType, 1, 1));
                    Assert.AreEqual(2, Stored(),
                        "a stale lower total overwrote a higher one");

                }

                //and only ever one row for the pair - read once the queue has let the file go
                Assert.AreEqual(1, RowCount(connectionInfo.ConnectionString, queueName, 1),
                    "a second error row was written for the same message and exception type");

                creator.RemoveQueue();
            }
        }

        private static int RowCount(string connectionString, string queueName, int queueId)
        {
            return Rows(connectionString, queueName, queueId).Count();
        }

        private static System.Collections.Generic.IEnumerable<Schema.ErrorTrackingTable> Rows(
            string connectionString, string queueName, int queueId)
        {
            using var db = new LiteDatabase(connectionString);
            return db.GetCollection<Schema.ErrorTrackingTable>($"{queueName}ErrorTracking")
                .Query()
                .Where(x => x.QueueId == queueId)
                .Where(x => x.ExceptionType == ExceptionType)
                .ToList();
        }
    }
}
