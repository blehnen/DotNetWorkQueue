using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    public class MoveRecordToErrorQueueCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            const int id = 19334;
            var error = new Exception();
            var context = Substitute.For<IMessageContext>();
            var test = new MoveRecordToErrorQueueCommand(error, id, context);
            Assert.Equal(id, test.QueueId);
            Assert.Equal(error, test.Exception);
            Assert.Equal(context, test.MessageContext);
        }
    }
}
