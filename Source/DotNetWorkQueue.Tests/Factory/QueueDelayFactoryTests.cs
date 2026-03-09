using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class QueueDelayFactoryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var factory = Create();
            var test = factory.Create();
            Assert.IsNotNull(test);
        }
        [TestMethod]
        public void Create_Default_TimeSpans()
        {
            var factory = Create();
            var list = new List<TimeSpan>
            {
                TimeSpan.FromHours(1),
                TimeSpan.FromHours(2),
                TimeSpan.FromHours(3)
            };
            var test = factory.Create(list);
            var i = 0;
            foreach (var t in test)
            {
                Assert.AreEqual(t, TimeSpan.FromHours(i + 1));
                i++;
            }
        }
        private IQueueDelayFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueDelayFactory>();
        }
    }
}
