// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

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
                () => { Console.WriteLine(string.Empty);});
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
