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
using System;
using DotNetWorkQueue.Factory;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Factory
{
    public class MessageContextDataFactoryTests
    {
        [Fact]
        public void Create_Null_Name_Fails()
        {
            var factory = Create();
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                factory.Create(null, new Data());
            });
        }
        [Theory, AutoData]
        public void Create_MessageContext(string value, Data data)
        {
            var factory = Create();
            var test = factory.Create(value, data);
            Assert.Equal(test.Name, value);
            Assert.Equal(test.Default, data);
        }

        [Theory, AutoData]
        public void Create_MessageContext_Null_Default(string value)
        {
            var factory = Create();
            var test = factory.Create<Data>(value, null);
            Assert.Equal(test.Default, null);
        }
        private IMessageContextDataFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageContextDataFactory>();
        }
        public class Data
        {
            
        }
    }
}
