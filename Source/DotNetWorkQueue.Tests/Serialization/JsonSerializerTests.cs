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
using DotNetWorkQueue.Serialization;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Serialization
{
    public class JsonSerializerTests
    {
        [Fact]
        public void ConvertMessageToBytes_Null_Exception()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
           delegate
           {
               test.ConvertMessageToBytes<Uri>(null);
           });
        }
        [Fact]
        public void BytesToMessage_Null_Exception()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
           delegate
           {
               test.ConvertBytesToMessage<Uri>(null);
           });
        }

        [Fact]
        public void Test_Serialization()
        {
            var test = Create();

            var testData = new TestData {Data = new byte[1000000]};
            var r = new Random();
            r.NextBytes(testData.Data);

            var serializedBytes = test.ConvertMessageToBytes(testData);
            var testData2 = test.ConvertBytesToMessage<TestData>(serializedBytes);

            Assert.Equal(testData.Data, testData2.Data);
        }

        [Fact]
        public void Test_Serialization_With_Interface()
        {
            var test = Create();

            ITestData testData = new TestData { Data = new byte[1000000] };
            var r = new Random();
            r.NextBytes(testData.Data);

            var serializedBytes = test.ConvertMessageToBytes(testData);
            var testData2 = test.ConvertBytesToMessage<ITestData>(serializedBytes);

            Assert.IsAssignableFrom<TestData>(testData2);
        }

        private ISerializer Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<JsonSerializer>();
        }

        private interface ITestData
        {
            byte[] Data { get; set; }
        }
        private class TestData : ITestData
        {
            public byte[] Data { get; set; }
        }
    }
}
