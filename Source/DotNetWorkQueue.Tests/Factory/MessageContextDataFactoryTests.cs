using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class MessageContextDataFactoryTests
    {
        [TestMethod]
        public void Create_Null_Name_Fails()
        {
            var factory = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
            delegate
            {
                factory.Create(null, new Data());
            });
        }
        [TestMethod]
        public void Create_MessageContext()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var data = fixture.Create<Data>();
            var factory = Create();
            var test = factory.Create(value, data);
            Assert.AreEqual(test.Name, value);
            Assert.AreEqual(test.Default, data);
        }

        [TestMethod]
        public void Create_MessageContext_Null_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var factory = Create();
            var test = factory.Create<Data>(value, null);
            Assert.IsNull(test.Default);
        }
        private IMessageContextDataFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageContextDataFactory>();
        }
        [TestClass]
        public class Data
        {

        }
    }
}
