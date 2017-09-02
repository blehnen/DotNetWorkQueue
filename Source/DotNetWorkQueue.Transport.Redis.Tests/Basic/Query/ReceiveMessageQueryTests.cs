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

using DotNetWorkQueue.Transport.Redis.Basic.Query;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Query
{
    public class ReceiveMessageQueryTests
    {
        [Fact]
        public void Create_Null_Constructor_Ok_For_ID()
        {
            var test = new ReceiveMessageQuery(Substitute.For<IMessageContext>(), null);
            Assert.Null(test.MessageId);
        }

        [Fact]
        public void Create_Default()
        {
            var context = Substitute.For<IMessageContext>();
            var id = Substitute.For<IMessageId>();
            var test = new ReceiveMessageQuery(context,id);
            Assert.Equal(id, test.MessageId);
            Assert.Equal(context, test.MessageContext);
        }
    }
}
