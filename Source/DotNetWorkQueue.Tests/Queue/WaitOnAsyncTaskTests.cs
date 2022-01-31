using System;
using System.Diagnostics;
using DotNetWorkQueue.Queue;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class WaitOnAsyncTaskTests
    {
        [Fact]
        public void Wait_None()
        {
            WaitOnAsyncTask.Wait(() => 1 == 2,
                () => { });
        }
        [Fact]
        public void Wait_Short()
        {
            var wait = new WaitSometime();
            WaitOnAsyncTask.Wait(() => wait.Wait(1000),
                () => { });
        }

        [Fact]
        public void Wait_Long()
        {
            var wait = new WaitSometime();
            WaitOnAsyncTask.Wait(() => wait.Wait(7000),
                () => { Console.WriteLine(string.Empty); });
        }

        private class WaitSometime
        {
            private Stopwatch _watch;
            public bool Wait(int time)
            {
                if (_watch != null) return _watch.ElapsedMilliseconds < time;

                _watch = new Stopwatch();
                _watch.Start();

                return _watch.ElapsedMilliseconds < time;
            }
        }
    }
}
