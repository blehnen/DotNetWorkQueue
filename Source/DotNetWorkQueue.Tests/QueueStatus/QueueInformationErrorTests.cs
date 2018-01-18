using System;
using System.Linq;
using AutoFixture.Xunit2;
using DotNetWorkQueue.QueueStatus;

using Xunit;

namespace DotNetWorkQueue.Tests.QueueStatus
{
    public class QueueInformationErrorTests
    {
        [Theory, AutoData]
        public void Create(string name, string server, Exception error)
        {
            IQueueInformation test = new QueueInformationError(name, server, error);
            Assert.Equal(name, test.Name);
            Assert.Equal(server, test.Server);
            Assert.Equal(DateTime.MinValue, test.CurrentDateTime);
            Assert.Equal(string.Empty, test.DateTimeProvider);
            Assert.Single(test.Data);
            var systemEntries = test.Data.ToList();
            Assert.Contains(error.ToString(), systemEntries[0].Value);
            Assert.Contains("Error", systemEntries[0].Name);
        }
    }
}
