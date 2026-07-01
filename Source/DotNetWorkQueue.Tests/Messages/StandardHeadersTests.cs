using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Messages;
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
            Assert.AreEqual("Queue-MessageBodyType", sut.MessageBodyType.Name);
        }

        [TestMethod]
        public void MessageBodyType_Default_Is_Null()
        {
            var sut = Create();
            Assert.IsNull(sut.MessageBodyType.Default);
        }

        private static IStandardHeaders Create()
        {
            return new StandardHeaders(new MessageContextDataFactory());
        }
    }
}
