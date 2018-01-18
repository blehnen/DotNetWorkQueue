using System;
using DotNetWorkQueue.Exceptions;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Exceptions
{
    public class MessageExceptionTests
    {
        [Fact]
        public void Create_Empty()
        {
            var e = new MessageException();
            Assert.Equal("Exception of type 'DotNetWorkQueue.Exceptions.MessageException' was thrown.", e.Message);
        }
        [Fact]
        public void Create()
        {
            var e = new MessageException("error", Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>());
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.MessageId);
            Assert.NotNull(e.CorrelationId);
        }
        [Fact]
        public void Create_Format()
        {
            var e = new MessageException(Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), "error {0}", 1);
            Assert.Equal("error 1", e.Message);
            Assert.NotNull(e.MessageId);
            Assert.NotNull(e.CorrelationId);
        }
        [Fact]
        public void Create_Inner()
        {
            var e = new MessageException("error", new Exception(), Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>());
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.InnerException);
            Assert.NotNull(e.MessageId);
            Assert.NotNull(e.CorrelationId);
        }
    }
}
