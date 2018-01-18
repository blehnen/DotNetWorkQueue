using System;
using System.Diagnostics;
using DotNetWorkQueue.Queue;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class WaitForDelegateTests
    {
        [Fact]
        public void Test_Timeout()
        {
            var timer = new Stopwatch();
            timer.Start();
            // ReSharper disable once EqualExpressionComparison
            WaitForDelegate.Wait(() => 1 == 1, TimeSpan.FromMilliseconds(1000));
            timer.Stop();
            Assert.InRange(timer.ElapsedMilliseconds, 1000, 1575);
        }
        [Fact]
        public void Test_NoTimeout()
        {
            WaitForDelegate.Wait(() => 1 == 2);
        }
    }
}
