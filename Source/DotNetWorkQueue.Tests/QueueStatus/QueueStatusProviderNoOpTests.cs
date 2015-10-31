// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using DotNetWorkQueue.QueueStatus;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.Tests.QueueStatus
{
    public class QueueStatusProviderNoOpTests
    {
        [Theory, AutoData]
        public void Create_Default(string name, string connection, string path)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var conn = fixture.Create<IConnectionInformation>();
            conn.QueueName = name;
            conn.ConnectionString = connection;
            fixture.Inject(conn);
            var test = fixture.Create<QueueStatusProviderNoOp>();
            Assert.NotNull(test.Current);
            Assert.Null(test.Error);
            Assert.Equal(name, test.Name);
            Assert.Null(test.HandlePath(path));
            Assert.Equal(string.Empty, test.Server);
        }
    }
}
