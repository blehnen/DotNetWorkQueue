// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Threading.Tasks;
using DotNetWorkQueue.IntegrationTests.Shared;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("SQLite")]
    public class MultiMethodConsumerAsync
    {
        [Theory]
        [InlineData(250, 1, 400, 10, 5, 5, true, LinqMethodTypes.Dynamic),
         InlineData(35, 5, 200, 10, 1, 2, true, LinqMethodTypes.Dynamic),
         InlineData(10, 8, 180, 7, 1, 1, true, LinqMethodTypes.Dynamic),
         InlineData(100, 0, 180, 10, 5, 0, true, LinqMethodTypes.Dynamic),
         InlineData(250, 1, 400, 10, 5, 5, false, LinqMethodTypes.Dynamic),
         InlineData(35, 5, 200, 10, 1, 2, false, LinqMethodTypes.Dynamic),
         InlineData(10, 8, 180, 7, 1, 1, false, LinqMethodTypes.Dynamic),
         InlineData(100, 0, 180, 10, 5, 0, false, LinqMethodTypes.Dynamic),
            InlineData(250, 1, 400, 10, 5, 5, true, LinqMethodTypes.Compiled),
         InlineData(35, 5, 200, 10, 1, 2, true, LinqMethodTypes.Compiled),
         InlineData(10, 8, 180, 7, 1, 1, true, LinqMethodTypes.Compiled),
         InlineData(100, 0, 180, 10, 5, 0, true, LinqMethodTypes.Compiled),
         InlineData(250, 1, 400, 10, 5, 5, false, LinqMethodTypes.Compiled),
         InlineData(35, 5, 200, 10, 1, 2, false, LinqMethodTypes.Compiled),
         InlineData(10, 8, 180, 7, 1, 1, false, LinqMethodTypes.Compiled),
         InlineData(100, 0, 180, 10, 5, 0, false, LinqMethodTypes.Compiled)]
        public void Run(int messageCount, int runtime, int timeOut, 
            int workerCount, int readerCount, int queueSize, bool inMemoryDb, LinqMethodTypes linqMethodTypes)
        {
            var factory = SimpleMethodConsumerAsync.CreateFactory(workerCount, queueSize);
            var task1 =
                Task.Factory.StartNew(
                    () =>
                        RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                            queueSize, 1, inMemoryDb, factory, linqMethodTypes));

            var task2 =
                Task.Factory.StartNew(
                    () =>
                        RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                            queueSize, 2, inMemoryDb, factory, linqMethodTypes));

            var task3 =
                Task.Factory.StartNew(
                    () =>
                        RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                            queueSize, 3, inMemoryDb, factory, linqMethodTypes));

            Task.WaitAll(task1, task2, task3);
        }

        private void RunConsumer(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType, bool inMemoryDb, ITaskFactory factory, LinqMethodTypes linqMethodTypes)
        {
            var queue = new SimpleMethodConsumerAsync();
            queue.Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, inMemoryDb, factory, linqMethodTypes);
        }
    }
}
