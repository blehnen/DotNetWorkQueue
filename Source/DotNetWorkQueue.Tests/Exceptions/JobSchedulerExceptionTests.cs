using System;
using DotNetWorkQueue.Exceptions;
using Xunit;

namespace DotNetWorkQueue.Tests.Exceptions
{
    public class JobSchedulerExceptionTests
    {
        [Fact()]
        public void JobSchedulerException_Test()
        {
            var e = new JobSchedulerException();
            Assert.Equal("Exception of type 'DotNetWorkQueue.Exceptions.JobSchedulerException' was thrown.", e.Message);
        }

        [Fact()]
        public void JobSchedulerException_Test1()
        {
            var e = new JobSchedulerException("error");
            Assert.Equal("error", e.Message);
        }

        [Fact()]
        public void JobSchedulerException_Test2()
        {
            var e = new JobSchedulerException("error {0}", 1);
            Assert.Equal("error 1", e.Message);
        }

        [Fact()]
        public void JobSchedulerException_Test3()
        {
            var e = new JobSchedulerException("error", new Exception());
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.InnerException);
        }
    }
}