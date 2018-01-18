using System;
using System.Text;
using DotNetWorkQueue.Interceptors;
using Xunit;

namespace DotNetWorkQueue.Tests.Interceptors
{
    public class TripleDesInterceptorTest
    {
        private readonly TripleDesMessageInterceptor _tripleDesMessageInterceptor = new TripleDesMessageInterceptor(new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Convert.FromBase64String("aaaaaaaaaaa=")));
        private readonly Random _random = new Random();

        [Theory]
        [InlineData(1, 8, 10),
        InlineData(100000, 1000000, 10),
        InlineData(1000000, 1000000, 1)]
        public void ShouldEncryptAndDecrypt(int minLength,
           int maxLength,
           int count)
        {
            foreach (var body in Helpers.RandomStrings(minLength, maxLength, count, _random))
            {
                TestDes(body);
            }
        }
        private void TestDes(string body)
        {
            var serialization = _tripleDesMessageInterceptor.MessageToBytes(Encoding.UTF8.GetBytes(body));
            var actual = Encoding.UTF8.GetString(_tripleDesMessageInterceptor.BytesToMessage(serialization.Output));
            Assert.Equal(body, actual);
        }
    }
}
