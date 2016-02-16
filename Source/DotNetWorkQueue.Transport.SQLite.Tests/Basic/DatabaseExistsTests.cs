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
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    public class DatabaseExistsTests
    {
        private const string BadConnection =
           "Thisisabadconnectionstring";

        private const string GoodConnectionInMemory = "FullUri=file:test.db3?mode=memory&cache=shared;Version=3;";

        [Fact]
        public void DatabaseDoesNotExist()
        {
            Assert.False(DatabaseExists.Exists(BadConnection));
        }

        [Fact]
        public void DatabaseDoesExist()
        {
            var fileName = System.IO.Path.GetTempFileName();
            var connectionString = $"Data Source={fileName};Version=3;";
            try
            {
                Assert.True(DatabaseExists.Exists(connectionString));
            }
            finally
            {
                System.IO.File.Delete(fileName);
            }
        }
        [Fact]
        public void DatabaseInMemory()
        {
            Assert.True(DatabaseExists.Exists(GoodConnectionInMemory));
        }
    }
}
