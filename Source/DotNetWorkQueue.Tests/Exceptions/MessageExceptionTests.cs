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
using DotNetWorkQueue.Exceptions;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Exceptions
{
    public class MessageExceptionTests
    {
        [Fact]
        public void Create_Empty()
        {
            var e = new MessageException();
            Assert.Equal("Exception of type 'DotNetWorkQueue.Exceptions.MessageException' was thrown.", e.Message);
        }
        [Fact]
        public void Create()
        {
            var e = new MessageException("error", Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>());
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.MessageId);
            Assert.NotNull(e.CorrelationId);
        }
        [Fact]
        public void Create_Format()
        {
            var e = new MessageException(Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>(), "error {0}", 1);
            Assert.Equal("error 1", e.Message);
            Assert.NotNull(e.MessageId);
            Assert.NotNull(e.CorrelationId);
        }
        [Fact]
        public void Create_Inner()
        {
            var e = new MessageException("error", new Exception(), Substitute.For<IMessageId>(), Substitute.For<ICorrelationId>());
            Assert.Equal("error", e.Message);
            Assert.NotNull(e.InnerException);
            Assert.NotNull(e.MessageId);
            Assert.NotNull(e.CorrelationId);
        }
    }
}
