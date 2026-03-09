// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.CommandHandler;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.CommandHandler
{
    [TestClass]
    public class DashboardResetStaleMessageCommandHandlerTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var resetLua = new DashboardResetStaleMessageLua(connection, redisNames);
            Assert.IsNotNull(new DashboardResetStaleMessageCommandHandler(resetLua));
        }

        [TestMethod]
        public void Create_NullResetLua_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DashboardResetStaleMessageCommandHandler(null));
        }
    }
}
