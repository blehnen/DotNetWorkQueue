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

using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Factory;
using DotNetWorkQueue.Transport.Redis.Basic.MessageID;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Factory
{
    public class GetMessageIdFactoryTests
    {
        [Fact]
        public void Create()
        {
            var redisId = new GetRedisIncrId();
            var uuId = new GetUuidMessageId();

            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var container = Substitute.For<IContainer>();
            container.GetInstance<GetRedisIncrId>().Returns(redisId);
            container.GetInstance<GetUuidMessageId>().Returns(uuId);
            fixture.Inject(container);

            var factory = fixture.Create<IContainerFactory>();
            factory.Create().ReturnsForAnyArgs(container);

            var options = Helpers.CreateOptions();
            options.MessageIdLocation = MessageIdLocations.RedisIncr;
            var test = new GetMessageIdFactory(factory, options);
            var result = test.Create();
            Assert.IsAssignableFrom<GetRedisIncrId>(result);

            options.MessageIdLocation = MessageIdLocations.Uuid;
            result = test.Create();
            Assert.IsAssignableFrom<GetUuidMessageId>(result);

            options.MessageIdLocation = MessageIdLocations.Custom;
            test.Create();

            options.MessageIdLocation = (MessageIdLocations)99;
            Assert.Throws<DotNetWorkQueueException>(
           delegate
           {
               test.Create();
           });
        }
    }
}
