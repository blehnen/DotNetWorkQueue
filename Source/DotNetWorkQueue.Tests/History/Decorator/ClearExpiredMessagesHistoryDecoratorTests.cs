using System.Threading;
using DotNetWorkQueue.History.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.History.Decorator
{
    [TestClass]
    public class ClearExpiredMessagesHistoryDecoratorTests
    {
        [TestMethod]
        public void ClearMessages_Delegates_To_Inner_Handler()
        {
            var (decorator, inner) = CreateDecorator();
            var token = CancellationToken.None;
            inner.ClearMessages(token).Returns(5L);

            decorator.ClearMessages(token);

            inner.Received(1).ClearMessages(token);
        }

        [TestMethod]
        public void ClearMessages_Returns_Inner_Result()
        {
            var (decorator, inner) = CreateDecorator();
            var token = CancellationToken.None;
            inner.ClearMessages(token).Returns(7L);

            var result = decorator.ClearMessages(token);

            Assert.AreEqual(7L, result);
        }

        private static (IClearExpiredMessages decorator, IClearExpiredMessages inner) CreateDecorator()
        {
            var inner = Substitute.For<IClearExpiredMessages>();
            var decorator = new ClearExpiredMessagesHistoryDecorator(inner);
            return (decorator, inner);
        }
    }
}
