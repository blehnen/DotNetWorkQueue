using System;
using DotNetWorkQueue.Exceptions;
using Xunit;

namespace DotNetWorkQueue.Tests.Exceptions
{
    public class InterceptorExceptionTests
    {
        [Fact]
        public void Create_Empty()
        {
            var e = new InterceptorException();
            Assert.Equal("Exception of type 'DotNetWorkQueue.Exceptions.InterceptorException' was thrown.", e.Message);
        }
        [Fact]
        public void Create()
        {
            var e = new InterceptorException("error");
            Assert.Equal("error", e.Message);
        }
        [Fact]
        public void Create_Format()
        {
            var e = new InterceptorException("error {0}", 1);
            Assert.Equal("error 1", e.Message);
        }
        [Fact]
        public void Create_Inner()
        {
            var e = new InterceptorException("error", new Exception());
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.InnerException);
        }
    }
}
