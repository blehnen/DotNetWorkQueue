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
using DotNetWorkQueue.Transport.SQLite.Basic;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    public class SqLiteHoldConnectionTests
    {
        private const string GoodConnection =
            @"Data Source=c:\temp\test.db;Version=3;";

        private const string GoodConnectionInMemory = "FullUri=file:test.db3?mode=memory&cache=shared;Version=3;";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Create_Default()
        {
            using (var test = new SqLiteHoldConnection())
            {
                var connection = CreateConnection(GoodConnection);
                var connectionMem = CreateConnection(GoodConnectionInMemory);
                test.AddConnectionIfNeeded(connection);
                test.AddConnectionIfNeeded(connectionMem);
                test.Dispose();
            }
        }
        private IConnectionInformation CreateConnection(string connectionString)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var connection = fixture.Create<IConnectionInformation>();
            connection.QueueName.Returns("TestQueue");
            connection.ConnectionString.Returns(connectionString);
            return connection;
        }
    }
}
