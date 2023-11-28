using DotNetWorkQueue.Exceptions;
using System;
using Xunit;

namespace DotNetWorkQueue.Tests.Exceptions
{
    public class CommitExceptionTests
    {
        [Fact]
        public void Create_Empty()
        {
            var e = new CommitException();
            Assert.Equal("Exception of type 'DotNetWorkQueue.Exceptions.CommitException' was thrown.", e.Message);
        }
        [Fact]
        public void Create()
        {
            var e = new CommitException("error", null, null, null);
            Assert.Equal("error", e.Message);
        }
        [Fact]
        public void Create_Format()
        {
            var e = new CommitException("error {0}", null, null, null, 1);
            Assert.Equal("error 1", e.Message);
        }
        [Fact]
        public void Create_Inner()
        {
            var e = new CommitException("error", new Exception(), null, null, null);
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.InnerException);
        }
    }
}
