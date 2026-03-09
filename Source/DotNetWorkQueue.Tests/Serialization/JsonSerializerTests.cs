using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Serialization
{
    [TestClass]
    public class JsonSerializerTests
    {
        [TestMethod]
        public void ConvertMessageToBytes_Null_Exception()
        {
            var test = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
           delegate
           {
               test.ConvertMessageToBytes<Uri>(null, null);
           });
        }
        [TestMethod]
        public void BytesToMessage_Null_Exception()
        {
            var test = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
           delegate
           {
               test.ConvertBytesToMessage<Uri>(null, null);
           });
        }

        [TestMethod]
        public void Test_Serialization()
        {
            var test = Create();

            var testData = new TestData { Data = new byte[1000000] };
            var r = new Random();
            r.NextBytes(testData.Data);

            var serializedBytes = test.ConvertMessageToBytes(testData, null);
            var testData2 = test.ConvertBytesToMessage<TestData>(serializedBytes, null);

            CollectionAssert.AreEqual(testData.Data, testData2.Data);
        }

        [TestMethod]
        public void Test_Serialization_With_Interface()
        {
            var test = Create();

            ITestData testData = new TestData { Data = new byte[1000000] };
            var r = new Random();
            r.NextBytes(testData.Data);

            var serializedBytes = test.ConvertMessageToBytes(testData, null);
            var testData2 = test.ConvertBytesToMessage<ITestData>(serializedBytes, null);

            Assert.IsInstanceOfType<TestData>(testData2);
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
