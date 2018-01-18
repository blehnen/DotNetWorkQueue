using DotNetWorkQueue.Transport.Redis.Basic.Query;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Query
{
    public class ReceiveMessageQueryTests
    {
        [Fact]
        public void Create_Null_Constructor_Ok_For_ID()
        {
            var test = new ReceiveMessageQuery(Substitute.For<IMessageContext>(), null);
            Assert.Null(test.MessageId);
        }

        [Fact]
        public void Create_Default()
        {
            var context = Substitute.For<IMessageContext>();
            var id = Substitute.For<IMessageId>();
            var test = new ReceiveMessageQuery(context,id);
            Assert.Equal(id, test.MessageId);
            Assert.Equal(context, test.MessageContext);
        }
    }
}
