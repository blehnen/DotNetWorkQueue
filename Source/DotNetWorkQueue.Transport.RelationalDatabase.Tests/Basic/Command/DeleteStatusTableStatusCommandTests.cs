using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    public class DeleteStatusTableStatusCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            const int id = 19334;
            var test = new DeleteStatusTableStatusCommand(id);
            Assert.Equal(id, test.QueueId);
        }
    }
}
