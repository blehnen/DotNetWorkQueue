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

using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    public class TableNameHelperTests
    {
        [Fact]
        public void ValidNames()
        {
            var test = Create(true);
            Assert.Equal(test.QueueName, "testQueue");
            Assert.StartsWith("testQueue", test.MetaDataName);
            Assert.StartsWith("testQueue", test.StatusName);
            Assert.StartsWith("testQueue", test.ConfigurationName);
            Assert.StartsWith("testQueue", test.ErrorTrackingName);
            Assert.StartsWith("testQueue", test.MetaDataErrorsName);
        }

        [Fact]
        public void InValidNames()
        {
            var test = Create(false);
            Assert.Equal(test.QueueName, "");
            Assert.Equal("Error-Name-Not-Set", test.MetaDataName);
            Assert.Equal("Error-Name-Not-Set", test.StatusName);
            Assert.Equal("Error-Name-Not-Set", test.ConfigurationName);
            Assert.Equal("Error-Name-Not-Set", test.ErrorTrackingName);
            Assert.Equal("Error-Name-Not-Set", test.MetaDataErrorsName);
        }

        private TableNameHelper Create(bool validConnection)
        {
            var connectionInformation = Substitute.For<IConnectionInformation>();
            if (validConnection)
                connectionInformation.QueueName.Returns("testQueue");
           return new TableNameHelper(connectionInformation);
        }
    }
}
