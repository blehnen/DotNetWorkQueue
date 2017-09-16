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
    public class RpcContextFactoryTests
    {
        [Theory, AutoData]
        public void Create_Default(TimeSpan value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();

            var factory = Create(fixture);
            var info = factory.Create(messageId, value);

            Assert.Equal(info.MessageId, messageId);
            Assert.Equal(info.Timeout, value);
        }
        [Fact]
        public void Create_Default_TimeSpan_Null()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();

            var factory = Create(fixture);
            var info = factory.Create(messageId, null);

            Assert.Equal(info.MessageId, messageId);
            Assert.Null(info.Timeout);
        }
        [Fact]
        public void Create_Null_Params_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = Create(fixture);
            Assert.Throws<ArgumentNullException>(
               delegate
               {
                   factory.Create(null, null);
               });
        }
        private IRpcContextFactory Create(IFixture fixture)
        {
            return fixture.Create<RpcContextFactory>();
        }
    }
}
