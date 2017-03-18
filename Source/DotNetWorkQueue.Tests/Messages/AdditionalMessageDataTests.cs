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
using System.Linq;
using DotNetWorkQueue.Messages;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Messages
{
    public class AdditionalMessageDataTests
    {
        [Fact]
        public void SetAndGet_CorrelationId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();
            var correlationId = fixture.Create<ICorrelationId>();
            message.CorrelationId = correlationId;
            Assert.Equal(message.CorrelationId, correlationId);
        }

        [Fact]
        public void SetAndGet_Route()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();
            var route = fixture.Create<string>();
            message.Route = route;
            Assert.Equal(message.Route, route);
        }

        [Fact]
        public void SetAndGet_AdditionalMetaData()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();
            var messageData = fixture.Create<IAdditionalMetaData>();
            message.AdditionalMetaData.Add(messageData);
            Assert.Equal(message.AdditionalMetaData[0], messageData);
        }
        [Fact]
        public void SetAndGet_Headers()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();

            messageContextDataFactory.Create("Test", headerData)
                .Returns(new MessageContextData<HeaderData>("Test", headerData));

            var property = messageContextDataFactory.Create("Test", headerData);
            message.SetHeader(property, headerData);

            Assert.Equal(message.GetHeader(property), headerData);
        }

        [Fact]
        public void SetAndGet_Headers_RawAccess()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();

            messageContextDataFactory.Create("Test", headerData)
               .Returns(new MessageContextData<HeaderData>("Test", headerData));

            var property = messageContextDataFactory.Create("Test", headerData);
            message.SetHeader(property, headerData);

            Assert.Equal(message.Headers.Values.First(), headerData);
        }

        [Fact]
        public void GetSet_Headers_Default_Value()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<AdditionalMessageData>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            messageContextDataFactory.Create("Test", headerData).Returns(new MessageContextData<HeaderData>("Test", headerData));

            var property = messageContextDataFactory.Create("Test", headerData);
            var headerData2 = test.GetHeader(property);
            Assert.Equal(headerData2, headerData);

            var headerData3 = test.GetHeader(property);
            Assert.Equal(headerData2, headerData3);
        }

        public class HeaderData
        {
            
        }
    }
}
