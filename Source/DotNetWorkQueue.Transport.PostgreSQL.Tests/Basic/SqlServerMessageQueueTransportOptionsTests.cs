using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    public class PostgreSqlMessageQueueTransportOptionsTests
    {
        [Fact]
        public void Readonly()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            test.SetReadOnly();
            Assert.True(test.IsReadOnly);
        }
        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            Assert.False(test.IsReadOnly);
        }

        [Fact]
        public void GetSet_Priority()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnablePriority;
            test.EnablePriority = !c;
            Assert.Equal(!c, test.EnablePriority);
        }

        [Fact]
        public void GetSet_EnableHoldTransactionUntilMessageCommitted()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableHoldTransactionUntilMessageCommitted;
            test.EnableHoldTransactionUntilMessageCommitted = !c;
            Assert.Equal(!c, test.EnableHoldTransactionUntilMessageCommitted);
        }

        [Fact]
        public void GetSet_EnableStatus()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableStatus;
            test.EnableStatus = !c;
            Assert.Equal(!c, test.EnableStatus);
        }

        [Fact]
        public void GetSet_EnableHeartBeat()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableHeartBeat;
            test.EnableHeartBeat = !c;
            Assert.Equal(!c, test.EnableHeartBeat);
        }

        [Fact]
        public void GetSet_EnableDelayedProcessing()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableDelayedProcessing;
            test.EnableDelayedProcessing = !c;
            Assert.Equal(!c, test.EnableDelayedProcessing);
        }

        [Fact]
        public void GetSet_EnableStatusTable()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableStatusTable;
            test.EnableStatusTable = !c;
            Assert.Equal(!c, test.EnableStatusTable);
        }

        [Fact]
        public void GetSet_QueueType()
        {
            var test = new PostgreSqlMessageQueueTransportOptions {QueueType = QueueTypes.RpcReceive};
            Assert.Equal(QueueTypes.RpcReceive, test.QueueType);
        }

        [Fact]
        public void GetSet_EnableMessageExpiration()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableMessageExpiration;
            test.EnableMessageExpiration = !c;
            Assert.Equal(!c, test.EnableMessageExpiration);
        }

        [Fact]
        public void Validation()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            test.ValidConfiguration();
        }
    }
}
