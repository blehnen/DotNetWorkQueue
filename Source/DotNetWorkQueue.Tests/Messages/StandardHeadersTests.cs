using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Messages;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class StandardHeadersTests
    {
        [TestMethod]
        public void MessageBodyType_Has_Correct_Name()
        {
            var sut = Create();
            sut.MessageBodyType.Name.Should().Be("Queue-MessageBodyType");
        }

        [TestMethod]
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
