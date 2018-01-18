using System;
using DotNetWorkQueue.Exceptions;
using Xunit;

namespace DotNetWorkQueue.Tests.Exceptions
{
    public class SerializationExceptionTests
    {
        [Fact]
        public void Create_Empty()
        {
            var e = new SerializationException();
            Assert.Equal("Exception of type 'DotNetWorkQueue.Exceptions.SerializationException' was thrown.", e.Message);
        }
        [Fact]
        public void Create()
        {
            var e = new SerializationException("error");
            Assert.Equal("error", e.Message);
        }
        [Fact]
        public void Create_Format()
        {
            var e = new SerializationException("error {0}", 1);
            Assert.Equal("error 1", e.Message);
        }
        [Fact]
        public void Create_Inner()
        {
            var e = new SerializationException("error", new Exception());
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.InnerException);
        }
    }
}
