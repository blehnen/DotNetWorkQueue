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
using DotNetWorkQueue.Configuration;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Configuration
{
    public class BaseConnectionInformationTests
    {
        [Theory, AutoData]
        public void GetSet_Connection(string expected)
        {
            var test = new BaseConnectionInformation(string.Empty, expected);
            Assert.Equal(expected, test.ConnectionString);
        }
        [Theory, AutoData]
        public void GetSet_Queue(string expected)
        {
            var test = new BaseConnectionInformation(expected, string.Empty);
            Assert.Equal(expected, test.QueueName);
        }
        [Theory, AutoData]
        public void Test_Clone(string queue, string connection)
        {
            var test = new BaseConnectionInformation(queue, connection);
            var clone = (BaseConnectionInformation)test.Clone();

            Assert.Equal(test.ConnectionString, clone.ConnectionString);
            Assert.Equal(test.QueueName, clone.QueueName);
        }
    }
}
