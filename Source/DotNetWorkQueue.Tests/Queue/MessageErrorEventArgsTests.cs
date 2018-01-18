using System;
using DotNetWorkQueue.Queue;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class MessageErrorEventArgsTests
    {
        [Fact]
        public void Default()
        {
            var e = new Exception();
            var test = new MessageErrorEventArgs(e);
            Assert.Equal(e, test.Error);
        }
    }
}
