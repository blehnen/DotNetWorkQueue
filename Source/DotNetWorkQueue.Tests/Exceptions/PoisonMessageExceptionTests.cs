using DotNetWorkQueue.Exceptions;
using NSubstitute;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Tests.Exceptions
{
    [TestClass]
    public class PoisonMessageExceptionTests
    {
        [TestMethod]
        public void Create_Empty()
        {
            var e = new PoisonMessageException();
            Assert.AreEqual("Exception of type 'DotNetWorkQueue.Exceptions.PoisonMessageException' was thrown.", e.Message);
            Assert.IsNull(e.HeaderPayload);
            Assert.IsNull(e.MessagePayload);
        }
        [TestMethod]
        public void Create()
        {
            var e = new PoisonMessageException("error", Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null, null, null);
            Assert.AreEqual("error", e.Message);
        }
        [TestMethod]
        public void Create_Format()
        {
            var e = new PoisonMessageException(Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null, null, null, "error {0}", 1);
            Assert.AreEqual("error 1", e.Message);
        }
        [TestMethod]
        public void Create_Inner()
        {
            var e = new PoisonMessageException("error", new Exception(), Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null, null, null);
            Assert.AreEqual("error", e.Message);
            Assert.IsNotNull(e.InnerException);
        }

        [TestMethod]
        public void Create_MessagePayload()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<byte[]>();
            var e = new PoisonMessageException("error", Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null, message, null);
            Assert.AreEqual(message, e.MessagePayload);
        }

        [TestMethod]
        public void Create_HeaderPayload()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var header = fixture.Create<byte[]>();
            var e = new PoisonMessageException("error", Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), null, null, header);
            Assert.AreEqual(header, e.HeaderPayload);
        }
    }
}
