using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests
{
    /// <summary>
    /// Every transport asks this twice per message received, which is why it is a loop rather than
    /// a LINQ call over an interface.
    /// </summary>
    [TestClass]
    public class CancelWorkExtensionsTests
    {
        [TestMethod]
        public void False_When_No_Token_Is_Cancelled()
        {
            using var first = new CancellationTokenSource();
            using var second = new CancellationTokenSource();

            Assert.IsFalse(Create(first.Token, second.Token).AnyCancellationRequested());
        }

        [TestMethod]
        public void True_When_The_First_Token_Is_Cancelled()
        {
            using var first = new CancellationTokenSource();
            using var second = new CancellationTokenSource();
            first.Cancel();

            Assert.IsTrue(Create(first.Token, second.Token).AnyCancellationRequested());
        }

        [TestMethod]
        public void True_When_A_Later_Token_Is_Cancelled()
        {
            //the loop has to keep going rather than answer from the first entry
            using var first = new CancellationTokenSource();
            using var second = new CancellationTokenSource();
            second.Cancel();

            Assert.IsTrue(Create(first.Token, second.Token).AnyCancellationRequested());
        }

        [TestMethod]
        public void False_When_There_Are_No_Tokens()
        {
            Assert.IsFalse(Create().AnyCancellationRequested());
        }

        [TestMethod]
        public void False_When_The_List_Is_Null()
        {
            var cancelWork = Substitute.For<ICancelWork>();
            cancelWork.Tokens.Returns((List<CancellationToken>)null);

            Assert.IsFalse(cancelWork.AnyCancellationRequested());
        }

        [TestMethod]
        public void False_When_There_Is_Nothing_To_Ask()
        {
            Assert.IsFalse(((ICancelWork)null).AnyCancellationRequested());
        }

        private static ICancelWork Create(params CancellationToken[] tokens)
        {
            var cancelWork = Substitute.For<ICancelWork>();
            cancelWork.Tokens.Returns(new List<CancellationToken>(tokens));
            return cancelWork;
        }
    }
}
