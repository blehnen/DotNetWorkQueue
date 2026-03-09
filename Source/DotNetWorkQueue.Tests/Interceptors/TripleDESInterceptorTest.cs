using System;
using System.Collections.ObjectModel;
using System.Text;
using DotNetWorkQueue.Interceptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Interceptors
{
    [TestClass]
    public class TripleDesInterceptorTest
    {
        private readonly TripleDesMessageInterceptor _tripleDesMessageInterceptor = new TripleDesMessageInterceptor(new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Convert.FromBase64String("aaaaaaaaaaa=")));
        private readonly Random _random = new Random();

        [TestMethod]
        [DataRow(1, 8, 10),
        DataRow(100000, 1000000, 10),
        DataRow(1000000, 1000000, 1)]
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
            var serialization = _tripleDesMessageInterceptor.MessageToBytes(Encoding.UTF8.GetBytes(body), null);
            var actual = Encoding.UTF8.GetString(_tripleDesMessageInterceptor.BytesToMessage(serialization.Output, null));
            Assert.AreEqual(body, actual);
        }
    }
}
