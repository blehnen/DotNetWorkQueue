using System;
using DotNetWorkQueue.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Exceptions
{
    [TestClass]
    public class SerializationExceptionTests
    {
        [TestMethod]
        public void Create_Empty()
        {
            var e = new SerializationException();
            Assert.AreEqual("Exception of type 'DotNetWorkQueue.Exceptions.SerializationException' was thrown.", e.Message);
        }
        [TestMethod]
        public void Create()
        {
            var e = new SerializationException("error");
            Assert.AreEqual("error", e.Message);
        }
        [TestMethod]
        public void Create_Format()
        {
            var e = new SerializationException("error {0}", 1);
            Assert.AreEqual("error 1", e.Message);
        }
        [TestMethod]
        public void Create_Inner()
        {
            var e = new SerializationException("error", new Exception());
            Assert.AreEqual("error", e.Message);
            Assert.IsNotNull(e.InnerException);
        }
    }
}
