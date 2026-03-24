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
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.History
{
    [TestClass]
    public class SimpleHistoryTests
    {
        [TestMethod]
        [DataRow(5, IntegrationConnectionInfo.ConnectionTypes.Direct)]
        [DataRow(5, IntegrationConnectionInfo.ConnectionTypes.Memory)]
        [DataRow(5, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(int messageCount, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var test = new SimpleHistoryTest();
                test.Run<LiteDbMessageQueueInit, FakeMessage, LiteDbMessageQueueCreation>(
                    new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount,
                    x => { x.Options.EnableHistory = true; },
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
