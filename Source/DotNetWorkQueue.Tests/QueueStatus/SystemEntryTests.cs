using AutoFixture.Xunit2;
using DotNetWorkQueue.QueueStatus;

using Xunit;

namespace DotNetWorkQueue.Tests.QueueStatus
{
    public class SystemEntryTests
    {
        [Theory, AutoData]
        public void Create(string name, string value)
        {
            var test = new SystemEntry(name, value);
            Assert.Equal(name, test.Name);
            Assert.Equal(value, test.Value);
        }
    }
}
