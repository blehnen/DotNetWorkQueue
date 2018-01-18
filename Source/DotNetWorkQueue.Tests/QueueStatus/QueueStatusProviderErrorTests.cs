using System;
using AutoFixture.Xunit2;
using DotNetWorkQueue.QueueStatus;
using DotNetWorkQueue.Tests.IoC;

using Xunit;

namespace DotNetWorkQueue.Tests.QueueStatus
{
    public class QueueStatusProviderErrorTests
    {
        [Theory, AutoData]
        public void Create_Default(string name, string connection, Exception error)
        {
            var test = Create(name, connection, error);
            Assert.NotNull(test.Current);
            Assert.Equal(error, test.Error);
            Assert.Equal(name, test.Name);
            Assert.Contains("Base connection object cannot determine", test.Server);
        }
        private IQueueStatusProvider Create(string name, string connection, Exception error)
        {
            return new QueueStatusProviderError<CreateContainerTest.NoOpDuplexTransport>(name, connection,
                new QueueContainer<CreateContainerTest.NoOpDuplexTransport>(), error);
        }
    }
}
