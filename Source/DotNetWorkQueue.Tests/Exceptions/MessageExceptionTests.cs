using DotNetWorkQueue.Exceptions;
using NSubstitute;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Exceptions
{
    [TestClass]
    public class MessageExceptionTests
    {
        [TestMethod]
        public void Create_Empty()
        {
            var e = new MessageException();
            Assert.AreEqual("Exception of type 'DotNetWorkQueue.Exceptions.MessageException' was thrown.", e.Message);
        }
        [TestMethod]
        public void Create()
        {
            var e = new MessageException("error", Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null);
            Assert.AreEqual("error", e.Message);
            Assert.IsNotNull(e.MessageId);
            Assert.IsNotNull(e.CorrelationId);
        }
        [TestMethod]
        public void Create_Format()
        {
            var e = new MessageException(Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null, "error {0}", 1);
            Assert.AreEqual("error 1", e.Message);
            Assert.IsNotNull(e.MessageId);
            Assert.IsNotNull(e.CorrelationId);
        }
        [TestMethod]
        public void Create_Inner()
        {
            var e = new MessageException("error", new Exception(), Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null);
            Assert.AreEqual("error", e.Message);
            Assert.IsNotNull(e.InnerException);
            Assert.IsNotNull(e.MessageId);
            Assert.IsNotNull(e.CorrelationId);
        }
    }
}
