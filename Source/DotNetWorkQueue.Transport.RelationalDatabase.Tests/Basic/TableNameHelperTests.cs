using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    public class TableNameHelperTests
    {
        [Fact]
        public void ValidNames()
        {
            var test = Create(true);
            Assert.Equal("testQueue", test.QueueName);
            Assert.StartsWith("testQueue", test.MetaDataName);
            Assert.StartsWith("testQueue", test.StatusName);
            Assert.StartsWith("testQueue", test.ConfigurationName);
            Assert.StartsWith("testQueue", test.ErrorTrackingName);
            Assert.StartsWith("testQueue", test.MetaDataErrorsName);
        }

        [Fact]
        public void InValidNames()
        {
            var test = Create(false);
            Assert.Equal("", test.QueueName);
            Assert.Equal("Error-Name-Not-Set", test.MetaDataName);
            Assert.Equal("Error-Name-Not-Set", test.StatusName);
            Assert.Equal("Error-Name-Not-Set", test.ConfigurationName);
            Assert.Equal("Error-Name-Not-Set", test.ErrorTrackingName);
            Assert.Equal("Error-Name-Not-Set", test.MetaDataErrorsName);
        }

        private TableNameHelper Create(bool validConnection)
        {
            var connectionInformation = Substitute.For<IConnectionInformation>();
            if (validConnection)
                connectionInformation.QueueName.Returns("testQueue");
           return new TableNameHelper(connectionInformation);
        }
    }
}
