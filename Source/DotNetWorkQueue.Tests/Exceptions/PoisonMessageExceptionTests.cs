using System;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Exceptions;
using NSubstitute;

using Xunit;

namespace DotNetWorkQueue.Tests.Exceptions
{
    public class PoisonMessageExceptionTests
    {
        [Fact]
        public void Create_Empty()
        {
            var e = new PoisonMessageException();
            Assert.Equal("Exception of type 'DotNetWorkQueue.Exceptions.PoisonMessageException' was thrown.", e.Message);
            Assert.Null(e.HeaderPayload);
            Assert.Null(e.MessagePayload);
        }
        [Fact]
        public void Create()
        {
            var e = new PoisonMessageException("error", Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null, null);
            Assert.Equal("error", e.Message);
        }
        [Fact]
        public void Create_Format()
        {
            var e = new PoisonMessageException(Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null, null, "error {0}", 1);
            Assert.Equal("error 1", e.Message);
        }
        [Fact]
        public void Create_Inner()
        {
            var e = new PoisonMessageException("error", new Exception(), Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null, null);
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.InnerException);
        }

        [Theory, AutoData]
        public void Create_MessagePayload(byte[] message)
        {
            var e = new PoisonMessageException("error", Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), message, null);
            Assert.Equal(message, e.MessagePayload);
        }

        [Theory, AutoData]
        public void Create_HeaderPayload(byte[] header)
        {
            var e = new PoisonMessageException("error", Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null, header);
            Assert.Equal(header, e.HeaderPayload);
        }
    }
}
