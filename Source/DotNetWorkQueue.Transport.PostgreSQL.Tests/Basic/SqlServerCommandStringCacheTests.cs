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
using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using FluentAssertions;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    public class PostgreSQLCommandStringCacheTests
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
            Assert.Equal("command", test.Get("test"));
        }

        [Fact]
        public void All_Commands_Set()
        {
            var test = Create();
            foreach (PostgreSqlCommandStringTypes command in Enum.GetValues(typeof(PostgreSqlCommandStringTypes)))
            {
                test.GetCommand(command).Should().NotBe(null, "All commands should be set {0}", command);
            }
        }

        [Fact]
        public void Threaded_Query()
        {
            var test = Create();

            var task1 = new Task(() => test.GetCommand(PostgreSqlCommandStringTypes.DeleteFromErrorTracking));
            var task2 = new Task(() => test.GetCommand(PostgreSqlCommandStringTypes.DeleteFromErrorTracking));
            var task3 = new Task(() => test.GetCommand(PostgreSqlCommandStringTypes.DeleteFromErrorTracking));
            var task4 = new Task(() => test.GetCommand(PostgreSqlCommandStringTypes.DeleteFromErrorTracking));
            var task5 = new Task(() => test.GetCommand(PostgreSqlCommandStringTypes.DeleteFromErrorTracking));

            task1.Start();
            task2.Start();
            task3.Start();
            task4.Start();
            task5.Start();

            Task.WaitAll(task1, task2, task3, task4, task5);
        }

        private PostgreSqlCommandStringCache Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var connection = fixture.Create<IConnectionInformation>();
            connection.QueueName.Returns("TestQueue");
            fixture.Inject(connection);
            return fixture.Create<PostgreSqlCommandStringCache>();
        }
    }
}
