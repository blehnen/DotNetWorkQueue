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

using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    public class SqlServerCommandStringCacheTests
    {
        [Fact]
        public void Create_Key_Missing()
        {
            var test = Create();
            Assert.False(test.Contains("test"));
        }

        [Fact]
        public void GetSet_Key()
        {
            var test = Create();
            test.Add("test", "command");
            Assert.True(test.Contains("test"));
            Assert.Equal("command", test.Get("test").CommandText);
        }

        [Fact]
        public void Threaded_Query()
        {
            var test = Create();

            var task1 = new Task(() => test.GetCommand(CommandStringTypes.DeleteFromErrorTracking));
            var task2 = new Task(() => test.GetCommand(CommandStringTypes.DeleteFromErrorTracking));
            var task3 = new Task(() => test.GetCommand(CommandStringTypes.DeleteFromErrorTracking));
            var task4 = new Task(() => test.GetCommand(CommandStringTypes.DeleteFromErrorTracking));
            var task5 = new Task(() => test.GetCommand(CommandStringTypes.DeleteFromErrorTracking));

            task1.Start();
            task2.Start();
            task3.Start();
            task4.Start();
            task5.Start();

            Task.WaitAll(task1, task2, task3, task4, task5);
        }

        private SqlServerCommandStringCache Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var connection = fixture.Create<IConnectionInformation>();
            connection.QueueName.Returns("TestQueue");
            fixture.Inject(connection);
            return fixture.Create<SqlServerCommandStringCache>();
        }
    }
}
