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

using System;
using DotNetWorkQueue.Transport.SqlServer.Basic.Command;
using NSubstitute;
using Xunit;
namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.Command
{
    public class MoveRecordToErrorQueueCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            const int id = 19334;
            var error = new Exception();
            var context = Substitute.For<IMessageContext>();
            var test = new MoveRecordToErrorQueueCommand(error, id, context);
            Assert.Equal(id, test.QueueId);
            Assert.Equal(error, test.Exception);
            Assert.Equal(context, test.MessageContext);
        }
    }
}
