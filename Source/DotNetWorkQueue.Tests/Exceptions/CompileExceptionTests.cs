using System;
using System.Runtime.Serialization;
using DotNetWorkQueue.Exceptions;
using Xunit;

namespace DotNetWorkQueue.Tests.Exceptions
{
    public class CompileExceptionTests
    {
        [Fact()]
        public void CompileException_Test()
        {
            var e = new CompileException("error", "code");
            Assert.Equal("error", e.Message);
            Assert.Equal("code", e.CompileCode);
        }

        [Fact()]
        public void CompileException_Test1()
        {
            var e = new CompileException("code", "error {0}", 1);
            Assert.Equal("error 1", e.Message);
            Assert.Equal("code", e.CompileCode);
        }

        [Fact()]
        public void CompileException_Test2()
        {
            var e = new CompileException("error", new Exception("errorinner"), "code");
            Assert.Equal("error", e.Message);
            Assert.Equal("code", e.CompileCode);
            Assert.Equal("errorinner", e.InnerException.Message);
        }

        [Fact()]
        public void GetObjectData_Test()
        {
            var e = new CompileException("error", "code");
            var info = new SerializationInfo(typeof(CompileException), new FormatterConverter());
            e.GetObjectData(info, new StreamingContext());
            Assert.Equal("code", info.GetString("CompileCode"));
            Assert.Equal("error", e.Message);
            Assert.Equal("code", e.CompileCode);
        }
    }
}