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
using System.Collections.Generic;
using DotNetWorkQueue.Factory;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Factory
{
    public class MessageFactoryTests
    {
        [Theory, AutoData]
        public void Create_Message(Data data, Dictionary<string, object> headers)
        {
            var factory = Create();
            var test = factory.Create(data, headers);
            Assert.Equal(test.Body, data);
            Assert.Equal(test.Headers, headers);
        }

        [Theory, AutoData]
        public void GetSet_Header(Data data, Dictionary<string, object> headers, HeaderData headerData)
        {
            var factory = Create();
            var test = factory.Create(data, headers);

            var messageContextDataFactory = CreateDataFactory();

            var property = messageContextDataFactory.Create("Test", headerData);
            test.SetHeader(property, headerData);

            var headerData2 = test.GetHeader(property);
            Assert.Equal(headerData2, headerData);
        }
        [Theory, AutoData]
        public void GetSet_InternalHeader(Data data, Dictionary<string, object> headers, HeaderData headerData)
        {
            var factory = Create();
            var test = factory.Create(data, headers);

            var messageContextDataFactory = CreateDataFactory();

            var property = messageContextDataFactory.Create("Test", headerData);
            test.SetInternalHeader(property, headerData);

            var headerData2 = test.GetInternalHeader(property);
            Assert.Equal(headerData2, headerData);
        }

        private IMessageFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageFactory>();
        }

        private IMessageContextDataFactory CreateDataFactory()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<IMessageContextDataFactory>();
        }
        public class Data
        {

        }
        public class HeaderData
        { }
    }
}
