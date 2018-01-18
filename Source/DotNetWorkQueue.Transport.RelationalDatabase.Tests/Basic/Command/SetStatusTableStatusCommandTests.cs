using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    public class SetStatusTableStatusCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            const int id = 19334;
            var test = new SetStatusTableStatusCommand(id, QueueStatuses.Processing);
            Assert.Equal(id, test.QueueId);
            Assert.Equal(QueueStatuses.Processing, test.Status);
        }
    }
}
