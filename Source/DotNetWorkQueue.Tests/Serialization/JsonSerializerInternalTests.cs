using System;
using DotNetWorkQueue.Serialization;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Serialization
{
    [TestClass]
    public class JsonSerializerInternalTests
    {
        [TestMethod]
        public void ConvertToBytes_Null_Exception()
        {
            var test = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
           delegate
           {
               test.ConvertToBytes<Uri>(null);
           });
        }
        [TestMethod]
        public void ConvertBytesTo_Null_Exception()
        {
            var test = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
           delegate
           {
               test.ConvertBytesTo<Uri>(null);
           });
        }
        [TestMethod]
        public void Test_Serialization()
        {
            var test = Create();

            var testData = new TestData { Data = new byte[1000000] };
            var r = new Random();
            r.NextBytes(testData.Data);

            var serializedBytes = test.ConvertToBytes(testData);
            var testData2 = test.ConvertBytesTo<TestData>(serializedBytes);

            CollectionAssert.AreEqual(testData.Data, testData2.Data);
        }

        [TestMethod]
        public void Test_Serialization_With_Interface_Exception()
        {
            var test = Create();

            ITestData testData = new TestData { Data = new byte[1000000] };
            var r = new Random();
            r.NextBytes(testData.Data);

            var serializedBytes = test.ConvertToBytes(testData);

            Assert.ThrowsExactly<JsonSerializationException>(
           delegate
           {
               var testData2 = test.ConvertBytesTo<ITestData>(serializedBytes);
               Assert.IsNull(testData2);
           });
        }

        [TestMethod]
        public void Deserialize_Denied_Type_Throws_JsonSerializationException()
        {
            var test = Create();
            var maliciousJson = "{\"$type\":\"System.Diagnostics.Process, System\"}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(maliciousJson);
            Assert.ThrowsExactly<JsonSerializationException>(
                delegate
                {
                    test.ConvertBytesTo<object>(bytes);
                });
        }

        private IInternalSerializer Create()
        {
            return new JsonSerializerInternal(new DenyListSerializationBinder());
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
