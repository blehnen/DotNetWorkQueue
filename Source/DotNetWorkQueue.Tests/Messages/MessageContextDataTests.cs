using System;
using DotNetWorkQueue.Messages;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class MessageContextDataTests
    {
        [TestMethod]
        public void Create_Null_Name_Fails()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
            delegate
            {
                var test = new MessageContextData<Data>(null, null);
                Assert.IsNull(test);
            });
        }

        [TestMethod]
        public void Create_Null_Data_Ok()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var test = new MessageContextData<Data>(name, null);
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void Create_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var d = fixture.Create<Data>();
            var test = new MessageContextData<Data>(name, d);
            Assert.AreEqual(name, test.Name);
            Assert.AreEqual(d, test.Default);
        }

        [TestClass]

        public class Data
        {

        }
    }
}
