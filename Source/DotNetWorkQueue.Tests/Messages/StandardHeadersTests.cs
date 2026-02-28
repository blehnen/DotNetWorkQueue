using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Messages;
using FluentAssertions;
using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class StandardHeadersTests
    {
        [Fact]
        public void MessageBodyType_Has_Correct_Name()
        {
            var sut = Create();
            sut.MessageBodyType.Name.Should().Be("Queue-MessageBodyType");
        }

        [Fact]
        public void MessageBodyType_Default_Is_Null()
        {
            var sut = Create();
            sut.MessageBodyType.Default.Should().BeNull();
        }

        private static IStandardHeaders Create()
        {
            return new StandardHeaders(new MessageContextDataFactory());
        }
    }
}
