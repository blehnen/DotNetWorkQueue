using System;
using System.Text;
using DotNetWorkQueue.Interceptors;
using Xunit;

namespace DotNetWorkQueue.Tests.Interceptors
{
    public class GzipInterceptorTest
    {
        private readonly Random _random = new Random();

        [Theory]
        [InlineData(140, 160, 50),
        InlineData(100000, 1000000, 10),
        InlineData(1000000, 10000000, 1)]
        public void ShouldCompressAndDecompress(int minLength,
           int maxLength,
           int count)
        {
            foreach (var body in Helpers.RandomStrings(minLength, maxLength, count, _random))
            {
                TestGzip(body);
            }
        }

        [Theory]
        [InlineData(8),
        InlineData(1000),
        InlineData(10000)]
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

            var serialization = gzip.MessageToBytes(Encoding.UTF8.GetBytes(body));
            if (serialization.AddToGraph)
            {
                var actual = Encoding.UTF8.GetString(gzip.BytesToMessage(serialization.Output));
                Assert.Equal(body, actual);
            }
            else
            {
                Assert.Equal(body, Encoding.UTF8.GetString(serialization.Output));
            }
        }

        private void TestGzipNoCompression(string body, int length)
        {
            var configuration = new GZipMessageInterceptorConfiguration {MinimumSize = length};
            var gzip = new GZipMessageInterceptor(configuration);

            var serialization = gzip.MessageToBytes(Encoding.UTF8.GetBytes(body));
            Assert.False(serialization.AddToGraph);
            Assert.Equal(body, Encoding.UTF8.GetString(serialization.Output));
        }
    }
}
