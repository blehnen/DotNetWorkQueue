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
using Xunit;
namespace DotNetWorkQueue.Transport.SQLite.Tests
{
    public class SqLiteConnectionInformationTests
    {
        private const string GoodConnection =
             @"Data Source=c:\temp\test.db;Version=3;";

        private const string BadConnection =
           "Thisisabadconnectionstring";

        [Fact]
        public void GetSet_Connection()
        {
            var test = new SqliteConnectionInformation { ConnectionString = GoodConnection};
            Assert.NotNull(test);
        }
        [Fact]
        public void Get_Server()
        {
            var test = new SqliteConnectionInformation { ConnectionString = GoodConnection };
            Assert.Equal(@"c:\temp\test.db", test.Server);
        }
        [Fact]
        public void GetSet_Connection_Bad_Exception()
        {
            var test = new SqliteConnectionInformation();
            Assert.Throws<ArgumentException>(
            delegate
            {
                test.ConnectionString = BadConnection;
            });
        }
        [Fact]
        public void Test_Clone()
        {
            var test = new SqliteConnectionInformation
            {
                QueueName = "blah",
                ConnectionString = GoodConnection
            };
            test.SetReadOnly();
            var clone = (SqliteConnectionInformation)test.Clone();

            Assert.Equal(test.ConnectionString, clone.ConnectionString);
            Assert.Equal(test.QueueName, clone.QueueName);
            Assert.NotEqual(test.IsReadOnly, clone.IsReadOnly);
        }
        [Fact]
        public void GetSet_Connection_Readonly()
        {
            var test = new SqliteConnectionInformation { ConnectionString = GoodConnection};
            test.SetReadOnly();
            Assert.Throws<InvalidOperationException>(
             delegate
             {
                 test.ConnectionString = "blah2";
             });
        }
    }
}
