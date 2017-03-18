// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    public class SqlServerMessageQueueTransportOptionsTests
    {
        [Fact]
        public void Readonly()
        {
            var test = new SqlServerMessageQueueTransportOptions();
            test.SetReadOnly();
            Assert.True(test.IsReadOnly);
        }
        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var test = new SqlServerMessageQueueTransportOptions();
            Assert.False(test.IsReadOnly);
        }

        [Fact]
        public void GetSet_Priority()
        {
            var test = new SqlServerMessageQueueTransportOptions();
            var c = test.EnablePriority;
            test.EnablePriority = !c;
            Assert.Equal(!c, test.EnablePriority);
        }

        [Fact]
        public void GetSet_EnableHoldTransactionUntilMessageCommited()
        {
            var test = new SqlServerMessageQueueTransportOptions();
            var c = test.EnableHoldTransactionUntilMessageCommited;
            test.EnableHoldTransactionUntilMessageCommited = !c;
            Assert.Equal(!c, test.EnableHoldTransactionUntilMessageCommited);
        }

        [Fact]
        public void GetSet_EnableStatus()
        {
            var test = new SqlServerMessageQueueTransportOptions();
            var c = test.EnableStatus;
            test.EnableStatus = !c;
            Assert.Equal(!c, test.EnableStatus);
        }

        [Fact]
        public void GetSet_EnableHeartBeat()
        {
            var test = new SqlServerMessageQueueTransportOptions();
            var c = test.EnableHeartBeat;
            test.EnableHeartBeat = !c;
            Assert.Equal(!c, test.EnableHeartBeat);
        }

        [Fact]
        public void GetSet_EnableDelayedProcessing()
        {
            var test = new SqlServerMessageQueueTransportOptions();
            var c = test.EnableDelayedProcessing;
            test.EnableDelayedProcessing = !c;
            Assert.Equal(!c, test.EnableDelayedProcessing);
        }

        [Fact]
        public void GetSet_EnableStatusTable()
        {
            var test = new SqlServerMessageQueueTransportOptions();
            var c = test.EnableStatusTable;
            test.EnableStatusTable = !c;
            Assert.Equal(!c, test.EnableStatusTable);
        }

        [Fact]
        public void GetSet_QueueType()
        {
            var test = new SqlServerMessageQueueTransportOptions {QueueType = QueueTypes.RpcReceive};
            Assert.Equal(QueueTypes.RpcReceive, test.QueueType);
        }

        [Fact]
        public void GetSet_EnableMessageExpiration()
        {
            var test = new SqlServerMessageQueueTransportOptions();
            var c = test.EnableMessageExpiration;
            test.EnableMessageExpiration = !c;
            Assert.Equal(!c, test.EnableMessageExpiration);
        }

        [Fact]
        public void Validation()
        {
            var test = new SqlServerMessageQueueTransportOptions();
            test.ValidConfiguration();
        }
    }
}
