using System;
using DotNetWorkQueue.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Exceptions
{
    [TestClass]
    public class InterceptorExceptionTests
    {
        [TestMethod]
        public void Create_Empty()
        {
            var e = new InterceptorException();
            Assert.AreEqual("Exception of type 'DotNetWorkQueue.Exceptions.InterceptorException' was thrown.", e.Message);
        }
        [TestMethod]
        public void Create()
        {
            var e = new InterceptorException("error");
            Assert.AreEqual("error", e.Message);
        }
        [TestMethod]
        public void Create_Format()
        {
            var e = new InterceptorException("error {0}", 1);
            Assert.AreEqual("error 1", e.Message);
        }
        [TestMethod]
        public void Create_Inner()
        {
            var e = new InterceptorException("error", new Exception());
            Assert.AreEqual("error", e.Message);
            Assert.IsNotNull(e.InnerException);
        }
    }
}
