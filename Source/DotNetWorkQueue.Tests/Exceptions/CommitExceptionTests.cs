using DotNetWorkQueue.Exceptions;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Exceptions
{
    [TestClass]
    public class CommitExceptionTests
    {
        [TestMethod]
        public void Create_Empty()
        {
            var e = new CommitException();
            Assert.AreEqual("Exception of type 'DotNetWorkQueue.Exceptions.CommitException' was thrown.", e.Message);
        }
        [TestMethod]
        public void Create()
        {
            var e = new CommitException("error", null, null, null);
            Assert.AreEqual("error", e.Message);
        }
        [TestMethod]
        public void Create_Format()
        {
            var e = new CommitException("error {0}", null, null, null, 1);
            Assert.AreEqual("error 1", e.Message);
        }
        [TestMethod]
        public void Create_Inner()
        {
            var e = new CommitException("error", new Exception(), null, null, null);
            Assert.AreEqual("error", e.Message);
            Assert.IsNotNull(e.InnerException);
        }
    }
}
