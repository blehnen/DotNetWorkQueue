using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Serialization;
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
