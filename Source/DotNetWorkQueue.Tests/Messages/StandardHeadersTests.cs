using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class StandardHeadersTests
    {
        [Fact]
        public void RpcConnectionInfo_Not_Null()
        {
            var test = Create();
            Assert.NotNull(test.RpcConnectionInfo);
        }

        [Fact]
        public void RpcConsumerException_Not_Null()
        {
            var test = Create();
            Assert.NotNull(test.RpcConsumerException);
        }

        [Fact]
        public void RpcContext_Not_Null()
        {
            var test = Create();
            Assert.NotNull(test.RpcContext);
        }

        [Fact]
        public void RpcResponseId_Not_Null()
        {
            var test = Create();
            Assert.NotNull(test.RpcResponseId);
        }

        [Fact]
        public void RpcTimeout_Not_Null()
        {
            var test = Create();
            Assert.NotNull(test.RpcTimeout);
        }

        private IStandardHeaders Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<StandardHeaders>();
        }
    }
}
