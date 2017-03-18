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
using DotNetWorkQueue.Factory;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Factory
{
    public class ResponseIdFactoryTests
    {
        [Theory, AutoData]
        public void Create_id(TimeSpan value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageid = fixture.Create<IMessageId>();
            var factory = Create(fixture);
            var id = factory.Create(messageid, value);

            Assert.Equal(id.MessageId, messageid);
            Assert.Equal(id.TimeOut, value);
        }

        [Theory, AutoData]
        public void Create_id_With_Null_MessageID_Fails(TimeSpan value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = Create(fixture);
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    factory.Create(null, value);
                });
        }

        private IResponseIdFactory Create(IFixture fixture)
        {
            return fixture.Create<ResponseIdFactory>();
        }
    }
}
