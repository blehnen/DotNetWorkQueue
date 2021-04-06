using System;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class MoveRecordToErrorQueueCommandTests
    {
        [Theory, AutoData]
        public void Create_Default(string number)
        {
            var error = new Exception();
            var context = Substitute.For<IMessageContext>();
            var test = new MoveRecordToErrorQueueCommand<string>(error, number, context);
            Assert.Equal(number, test.QueueId);
            Assert.Equal(error, test.Exception);
            Assert.Equal(context, test.MessageContext);
        }
    }
}
