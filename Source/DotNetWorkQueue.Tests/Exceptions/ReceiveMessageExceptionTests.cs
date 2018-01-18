using System;
using DotNetWorkQueue.Exceptions;
using Xunit;

namespace DotNetWorkQueue.Tests.Exceptions
{
    public class ReceiveMessageExceptionTests
    {
        [Fact]
        public void Create_Empty()
        {
            var e = new ReceiveMessageException();
            Assert.Equal("Exception of type 'DotNetWorkQueue.Exceptions.ReceiveMessageException' was thrown.", e.Message);
        }
        [Fact]
        public void Create()
        {
            var e = new ReceiveMessageException("error");
            Assert.Equal("error", e.Message);
        }
        [Fact]
        public void Create_Format()
        {
            var e = new ReceiveMessageException("error {0}", 1);
            Assert.Equal("error 1", e.Message);
        }
        [Fact]
        public void Create_Inner()
        {
            var e = new ReceiveMessageException("error", new Exception());
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.InnerException);
        }
    }
}
