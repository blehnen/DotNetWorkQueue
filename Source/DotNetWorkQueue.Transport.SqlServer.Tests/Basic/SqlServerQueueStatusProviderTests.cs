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
using System.Linq;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    public class SqlServerQueueStatusProviderTests
    {
        [Fact]
        public void GetData()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = fixture.Create<ISqlServerMessageQueueTransportOptionsFactory>();
            factory.Create().Returns(fixture.Create<SqlServerMessageQueueTransportOptions>());
            fixture.Inject(factory);
            var test = fixture.Create<SqlServerQueueStatusProvider>();
            Assert.Equal(5, test.Current.Data.Count());
        }
    }
}
