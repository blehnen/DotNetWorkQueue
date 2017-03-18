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
            var configuration = new GZipMessageInterceptorConfiguration {MiniumSize = length};
            var gzip = new GZipMessageInterceptor(configuration);

            var serialization = gzip.MessageToBytes(Encoding.UTF8.GetBytes(body));
            Assert.Equal(false, serialization.AddToGraph);
            Assert.Equal(body, Encoding.UTF8.GetString(serialization.Output));
        }
    }
}
