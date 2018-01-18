using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    public class CorrelationIdTests
    {
        [Fact]
        public void Create_Default()
        {
            var id = Guid.NewGuid();
            var test = new MessageCorrelationId(id);
            Assert.Equal(id, test.Id.Value);
            Assert.True(test.HasValue);
        }
        [Fact]
        public void Create_Default_ToString()
        {
            var id = Guid.NewGuid();
            var test = new MessageCorrelationId(id);
            Assert.Equal(id.ToString(), test.ToString());
        }
        [Fact]
        public void Create_Default_Empty_Guid()
        {
            var id = Guid.Empty;
            var test = new MessageCorrelationId(id);
            Assert.Equal(id, test.Id.Value);
            Assert.False(test.HasValue);
        }
    }
}
