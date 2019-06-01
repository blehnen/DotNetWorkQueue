using DotNetWorkQueue.Transport.Redis.Basic.Query;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Query
{
    public class ReceiveMessageQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var context = Substitute.For<IMessageContext>();
            var test = new ReceiveMessageQuery(context);
            Assert.Equal(context, test.MessageContext);
        }
    }
}
