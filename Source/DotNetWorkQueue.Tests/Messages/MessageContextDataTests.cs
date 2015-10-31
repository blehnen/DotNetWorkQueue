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
using DotNetWorkQueue.Messages;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageContextDataTests
    {
        [Fact]
        public void Create_Null_Name_Fails()
        {
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                var test = new MessageContextData<Data>(null, null);
                Assert.Null(test);
            });
        }

        [Theory, AutoData]
        public void Create_Null_Data_Ok(string name)
        {
            var test = new MessageContextData<Data>(name, null);
            Assert.NotNull(test);
        }

        [Theory, AutoData]
        public void Create_Default(string name, Data d)
        {
            var test = new MessageContextData<Data>(name, d);
            Assert.Equal(name, test.Name);
            Assert.Equal(d, test.Default);
        }

        public class Data
        {

        }
    }
}
