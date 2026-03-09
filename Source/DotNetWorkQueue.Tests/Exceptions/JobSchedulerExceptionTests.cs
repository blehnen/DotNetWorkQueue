using System;
using DotNetWorkQueue.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Exceptions
{
    [TestClass]
    public class JobSchedulerExceptionTests
    {
        [TestMethod]
        public void JobSchedulerException_Test()
        {
            var e = new JobSchedulerException();
            Assert.AreEqual("Exception of type 'DotNetWorkQueue.Exceptions.JobSchedulerException' was thrown.", e.Message);
        }

        [TestMethod]
        public void JobSchedulerException_Test1()
        {
            var e = new JobSchedulerException("error");
            Assert.AreEqual("error", e.Message);
        }

        [TestMethod]
        public void JobSchedulerException_Test2()
        {
            var e = new JobSchedulerException("error {0}", 1);
            Assert.AreEqual("error 1", e.Message);
        }

        [TestMethod]
        public void JobSchedulerException_Test3()
        {
            var e = new JobSchedulerException("error", new Exception());
            Assert.AreEqual("error", e.Message);
            Assert.IsNotNull(e.InnerException);
        }
    }
}