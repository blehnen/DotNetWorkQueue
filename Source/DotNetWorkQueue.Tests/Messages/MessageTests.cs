// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Collections.Generic;
using DotNetWorkQueue.Messages;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageTests
    {
        [Theory, AutoData]
        public void Create_Null_Constructor_Headers_Ok(Data data)
        {
            var test = new Message(data, null);
            Assert.NotNull(test);
        }
        [Theory, AutoData]
        public void GetSet_Header_Raw(Data data, Dictionary<string, object> headers)
        {
            var test = new Message(data, headers);

            Assert.Equal(test.Headers.Count, headers.Count);
            Assert.Equal(test.Headers, headers);
            Assert.NotSame(headers, test.Headers);
        }
        [Theory, AutoData]
        public void GetSet_Header(Data data, HeaderData headerData, string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = new Message(data);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var property = messageContextDataFactory.Create(value, headerData);
            test.SetHeader(property, headerData);

            var headerData2 = test.GetHeader(property);
            Assert.Equal(headerData2, headerData);
        }
        [Theory, AutoData]
        public void GetSet_InternalHeader(Data data, HeaderData headerData, string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = new Message(data);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var property = messageContextDataFactory.Create(value, headerData);
            test.SetInternalHeader(property, headerData);

            var headerData2 = test.GetInternalHeader(property);
            Assert.Equal(headerData2, headerData);
        }

        [Theory, AutoData]
        public void GetSet_Headers_Equal(Data data, HeaderData headerData, string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = new Message(data);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var property = messageContextDataFactory.Create(value, headerData);
            test.SetHeader(property, headerData);

            var headerData2 = test.GetHeader(property);
            Assert.Equal(headerData2, headerData);
        }

        [Theory, AutoData]
        public void GetSet_HeaderInternal_Default_Value(Data data, HeaderData headerData, string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = new Message(data);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            messageContextDataFactory.Create(value, headerData).Returns(new MessageContextData<HeaderData>(value, headerData));

            var property = messageContextDataFactory.Create(value, headerData);
            var headerData2 = test.GetInternalHeader(property);
            Assert.Equal(headerData2, headerData);

            var headerData3 = test.GetInternalHeader(property);
            Assert.Equal(headerData2, headerData3);
        }

        [Theory, AutoData]
        public void GetSet_Headers_Default_Value(Data data, HeaderData headerData, string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = new Message(data);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            messageContextDataFactory.Create(value, headerData).Returns(new MessageContextData<HeaderData>(value, headerData));

            var property = messageContextDataFactory.Create(value, headerData);
            var headerData2 = test.GetHeader(property);
            Assert.Equal(headerData2, headerData);

            var headerData3 = test.GetHeader(property);
            Assert.Equal(headerData2, headerData3);
        }

        public class Data
        {

        }
        public class HeaderData
        { }
    }
}
