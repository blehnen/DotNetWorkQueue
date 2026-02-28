// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Collections.Generic;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class AddStandardMessageHeadersTests
    {
        [Fact]
        public void AddHeaders_Stamps_MessageBodyType_As_PortableName()
        {
            var (sut, message) = CreateSut(new SimpleTestBody());

            sut.AddHeaders(message, Substitute.For<IAdditionalMessageData>());

            message.Headers.Should().ContainKey("Queue-MessageBodyType");
            var stamped = (string)message.Headers["Queue-MessageBodyType"];
            stamped.Should().Be($"{typeof(SimpleTestBody).FullName}, {typeof(SimpleTestBody).Assembly.GetName().Name}");
        }

        [Fact]
        public void AddHeaders_PortableName_Does_Not_Contain_Version()
        {
            var (sut, message) = CreateSut(new SimpleTestBody());

            sut.AddHeaders(message, Substitute.For<IAdditionalMessageData>());

            var stamped = (string)message.Headers["Queue-MessageBodyType"];
            stamped.Should().NotContain("Version=");
            stamped.Should().NotContain("Culture=");
            stamped.Should().NotContain("PublicKeyToken=");
        }

        [Fact]
        public void AddHeaders_Does_Not_Stamp_MessageBodyType_For_Delegate_Body()
        {
            Action<string> delegateBody = _ => { };
            var (sut, message) = CreateSut(delegateBody);

            sut.AddHeaders(message, Substitute.For<IAdditionalMessageData>());

            message.Headers.Should().NotContainKey("Queue-MessageBodyType");
        }

        [Fact]
        public void AddHeaders_Always_Stamps_FirstPossibleDeliveryDate()
        {
            var (sut, message) = CreateSut(new SimpleTestBody());

            sut.AddHeaders(message, Substitute.For<IAdditionalMessageData>());

            message.Headers.Should().ContainKey("Queue-FirstPossibleDeliveryDate");
        }

        private static (AddStandardMessageHeaders sut, IMessage message) CreateSut(dynamic body)
        {
            var deliveryTime = Substitute.For<IGetFirstMessageDeliveryTime>();
            deliveryTime.GetTime(Arg.Any<IMessage>(), Arg.Any<IAdditionalMessageData>())
                .Returns(DateTime.UtcNow);

            var standardHeaders = new StandardHeaders(new MessageContextDataFactory());

            var headers = Substitute.For<IHeaders>();
            headers.StandardHeaders.Returns(standardHeaders);

            var sut = new AddStandardMessageHeaders(headers, deliveryTime);
            var message = new Message(body, new Dictionary<string, object>());
            return (sut, message);
        }

        private class SimpleTestBody { }
    }
}
