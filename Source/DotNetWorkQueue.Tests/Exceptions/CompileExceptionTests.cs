using System;
using System.Runtime.Serialization;
using DotNetWorkQueue.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Exceptions
{
    [TestClass]
    public class CompileExceptionTests
    {
        [TestMethod]
        public void CompileException_Test()
        {
            var e = new CompileException("error", "code");
            Assert.AreEqual("error", e.Message);
            Assert.AreEqual("code", e.CompileCode);
        }

        [TestMethod]
        public void CompileException_Test1()
        {
            var e = new CompileException("code", "error {0}", 1);
            Assert.AreEqual("error 1", e.Message);
            Assert.AreEqual("code", e.CompileCode);
        }

        [TestMethod]
        public void CompileException_Test2()
        {
            var e = new CompileException("error", new Exception("errorinner"), "code");
            Assert.AreEqual("error", e.Message);
            Assert.AreEqual("code", e.CompileCode);
            Assert.AreEqual("errorinner", e.InnerException.Message);
        }

        [TestMethod]
        public void GetObjectData_Test()
        {
            var e = new CompileException("error", "code");
            var info = new SerializationInfo(typeof(CompileException), new FormatterConverter());
            e.GetObjectData(info, new StreamingContext());
            Assert.AreEqual("code", info.GetString("CompileCode"));
            Assert.AreEqual("error", e.Message);
            Assert.AreEqual("code", e.CompileCode);
        }
    }
}