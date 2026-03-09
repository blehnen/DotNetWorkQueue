using System;
using System.Text;
using DotNetWorkQueue.Interceptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Interceptors
{
    [TestClass]
    public class GzipInterceptorTest
    {
        private readonly Random _random = new Random();

        [TestMethod]
        [DataRow(140, 160, 50),
        DataRow(100000, 1000000, 10),
        DataRow(1000000, 10000000, 1)]
        public void ShouldCompressAndDecompress(int minLength,
           int maxLength,
           int count)
        {
            foreach (var body in Helpers.RandomStrings(minLength, maxLength, count, _random))
            {
                TestGzip(body);
            }
        }

        [TestMethod]
        [DataRow(8),
        DataRow(1000),
        DataRow(10000)]
        public void ShouldNotCompress(int length)
        {
            foreach (var body in Helpers.RandomStrings(length - 1, length - 1, 1, _random))
            {
                TestGzipNoCompression(body, length);
            }
        }

        private void TestGzip(string body)
        {
            var configuration = new GZipMessageInterceptorConfiguration();
            var gzip = new GZipMessageInterceptor(configuration);

            var serialization = gzip.MessageToBytes(Encoding.UTF8.GetBytes(body), null);
            if (serialization.AddToGraph)
            {
                var actual = Encoding.UTF8.GetString(gzip.BytesToMessage(serialization.Output, null));
                Assert.AreEqual(body, actual);
            }
            else
            {
                Assert.AreEqual(body, Encoding.UTF8.GetString(serialization.Output));
            }
        }

        private void TestGzipNoCompression(string body, int length)
        {
            var configuration = new GZipMessageInterceptorConfiguration { MinimumSize = length };
            var gzip = new GZipMessageInterceptor(configuration);

            var serialization = gzip.MessageToBytes(Encoding.UTF8.GetBytes(body), null);
            Assert.IsFalse(serialization.AddToGraph);
            Assert.AreEqual(body, Encoding.UTF8.GetString(serialization.Output));
        }
    }
}
