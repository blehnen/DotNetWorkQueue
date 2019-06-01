using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Tests.Basic
{
    public class SqLiteMessageQueueTransportOptionsTests
    {
        [Fact]
        public void Readonly()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            test.SetReadOnly();
            Assert.True(test.IsReadOnly);
        }
        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            Assert.False(test.IsReadOnly);
        }

        [Fact]
        public void GetSet_Priority()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnablePriority;
            test.EnablePriority = !c;
            Assert.Equal(!c, test.EnablePriority);
        }

        [Fact]
        public void GetSet_EnableStatus()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnableStatus;
            test.EnableStatus = !c;
            Assert.Equal(!c, test.EnableStatus);
        }

        [Fact]
        public void GetSet_EnableHeartBeat()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnableHeartBeat;
            test.EnableHeartBeat = !c;
            Assert.Equal(!c, test.EnableHeartBeat);
        }

        [Fact]
        public void GetSet_EnableDelayedProcessing()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnableDelayedProcessing;
            test.EnableDelayedProcessing = !c;
            Assert.Equal(!c, test.EnableDelayedProcessing);
        }

        [Fact]
        public void GetSet_EnableStatusTable()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnableStatusTable;
            test.EnableStatusTable = !c;
            Assert.Equal(!c, test.EnableStatusTable);
        }


        [Fact]
        public void GetSet_EnableMessageExpiration()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnableMessageExpiration;
            test.EnableMessageExpiration = !c;
            Assert.Equal(!c, test.EnableMessageExpiration);
        }

        [Fact]
        public void Validation()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            test.ValidConfiguration();
        }
    }
}
