using System;
using DotNetWorkQueue.Exceptions;
using Xunit;

namespace DotNetWorkQueue.Tests.Exceptions
{
    public class DotNetWorkQueueExceptionTests
    {
        [Fact]
        public void Create_Empty()
        {
            var e = new DotNetWorkQueueException();
            Assert.Equal("Exception of type 'DotNetWorkQueue.Exceptions.DotNetWorkQueueException' was thrown.", e.Message);
        }
        [Fact]
        public void Create()
        {
            var e = new DotNetWorkQueueException("error");
            Assert.Equal("error", e.Message);
        }
        [Fact]
        public void Create_Format()
        {
            var e = new DotNetWorkQueueException("error {0}", 1);
            Assert.Equal("error 1", e.Message);
        }
        [Fact]
        public void Create_Inner()
        {
            var e = new DotNetWorkQueueException("error", new Exception());
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.InnerException);
        }
    }
}
