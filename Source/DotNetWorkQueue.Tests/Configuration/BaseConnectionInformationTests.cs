using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class BaseConnectionInformationTests
    {
        [TestMethod]
        public void GetSet_Connection()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var expected = fixture.Create<string>();
            var test = new BaseConnectionInformation(new QueueConnection(string.Empty, expected));
            Assert.AreEqual(expected, test.ConnectionString);
        }
        [TestMethod]
        public void GetSet_Queue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var expected = fixture.Create<string>();
            var test = new BaseConnectionInformation(new QueueConnection(expected, string.Empty));
            Assert.AreEqual(expected, test.QueueName);
        }
        [TestMethod]
        public void Test_Clone()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            var settings = fixture.Create<Dictionary<string, string>>();
            var test = new BaseConnectionInformation(new QueueConnection(queue, connection, settings));
            var clone = (BaseConnectionInformation)test.Clone();

            Assert.AreEqual(test.ConnectionString, clone.ConnectionString);
            Assert.AreEqual(test.QueueName, clone.QueueName);

            foreach (var setting in settings)
            {
                Assert.IsTrue(clone.AdditionalConnectionSettings.ContainsKey(setting.Key));
            }
        }

        [TestMethod]
        public void Test_Equals()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            var settings = fixture.Create<Dictionary<string, string>>();
            var settings2 = fixture.Create<Dictionary<string, string>>();
            var test = new BaseConnectionInformation(new QueueConnection(queue, connection, settings));
            var clone = (BaseConnectionInformation)test.Clone();
            Assert.IsTrue(test.Equals(clone));
            Assert.IsFalse(test.Equals(null));

            var test2 = new BaseConnectionInformation(new QueueConnection(queue, connection));
            Assert.IsFalse(test2.Equals(test));

            var test3 = new BaseConnectionInformation(new QueueConnection(queue, connection, settings2));
            Assert.IsFalse(test2.Equals(test));
        }

        [TestMethod]
        public void Test_Server()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            var settings = fixture.Create<Dictionary<string, string>>();
            var test = new BaseConnectionInformation(new QueueConnection(queue, connection, settings));
            Assert.AreEqual("Base connection object cannot determine server", test.Server);
        }

        [TestMethod]
        public void Test_Container()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            var settings = fixture.Create<Dictionary<string, string>>();
            var test = new BaseConnectionInformation(new QueueConnection(queue, connection, settings));
            Assert.AreEqual("Base connection object cannot determine container", test.Container);
        }
    }
}
