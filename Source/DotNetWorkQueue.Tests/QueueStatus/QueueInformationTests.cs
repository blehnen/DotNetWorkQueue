using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture.Xunit2;
using DotNetWorkQueue.QueueStatus;

using Xunit;

namespace DotNetWorkQueue.Tests.QueueStatus
{
    public class QueueInformationTests
    {
        [Theory, AutoData]
        public void Create(string name,
            string server,
            DateTime currentDateTime,
            string dateTimeProvider,
            IEnumerable<SystemEntry> data)
        {
            var systemEntries = data as IList<SystemEntry> ?? data.ToList();
            IQueueInformation test = new QueueInformation(name, server, currentDateTime, dateTimeProvider, systemEntries);
            Assert.Equal(name, test.Name);
            Assert.Equal(server, test.Server);
            Assert.Equal(currentDateTime, test.CurrentDateTime);
            Assert.Equal(dateTimeProvider, test.DateTimeProvider);
            Assert.Equal(systemEntries, test.Data);
        }
    }
}