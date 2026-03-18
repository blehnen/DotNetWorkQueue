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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.History.Implementation;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.History
{
    [TestClass]
    public class SimpleHistoryTests
    {
        [TestMethod]
        [DataRow(5)]
        public void Run(int messageCount)
        {
            var queueName = GenerateQueueName.Create();
            var test = new SimpleHistoryTest();
            test.Run<SqlServerMessageQueueInit, FakeMessage, SqlServerMessageQueueCreation>(
                new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount,
                x => Helpers.SetOptions(x,
                    false, false, false,
                    false, false, false, true, false),
                Helpers.GenerateData, Helpers.Verify,
                scope => { });
        }
    }
}
