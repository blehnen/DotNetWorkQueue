using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.LiteDb.Tests
{
    [TestClass]
    public class LiteDbConnectionInformationTests
    {
        private const string GoodConnection =
            @"FileName=c:\temp\test.db;";

        [TestMethod]
        public void LiteDbConnectionInformation_Test()
        {
            var test = new LiteDbConnectionInformation(new QueueConnection("blah", GoodConnection));
            Assert.AreEqual("blah", test.QueueName);
            Assert.AreEqual(GoodConnection, test.ConnectionString);
        }

        [TestMethod]
        public void Clone_Test()
        {
            var test = new LiteDbConnectionInformation(new QueueConnection("blah", GoodConnection));
            var clone = test.Clone();

            Assert.AreEqual(test.ConnectionString, clone.ConnectionString);
            Assert.AreEqual(test.QueueName, clone.QueueName);
        }
    }
}