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
using DotNetWorkQueue.Messages;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class RpcContextTests
    {
        [Fact]
        public void Create_Constructor_Null_Timeout()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = new RpcContext(fixture.Create<IMessageId>(), null);
            Assert.NotNull(test);
        }

        [Fact]
        public void Get_MessageId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var test = new RpcContext(messageId, TimeSpan.Zero);
            Assert.Equal(test.MessageId, messageId);
        }

        [Theory, AutoData]
        public void Get_Timeout(TimeSpan value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = new RpcContext(fixture.Create<IMessageId>(), value);
            Assert.Equal(test.Timeout, value);
        }
    }
}
