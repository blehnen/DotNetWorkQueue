using System;
using System.Diagnostics;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WaitForDelegateTests
    {
        [TestMethod]
        public void Test_Timeout()
        {
            var timer = new Stopwatch();
            timer.Start();
            // ReSharper disable once EqualExpressionComparison
            WaitForDelegate.Wait(() => 1 == 1, TimeSpan.FromMilliseconds(1000));
            timer.Stop();
            Assert.IsInRange(1000L, 1575L, timer.ElapsedMilliseconds);
        }
        [TestMethod]
        public void Test_NoTimeout()
        {
            WaitForDelegate.Wait(() => 1 == 2);
        }
    }
}
