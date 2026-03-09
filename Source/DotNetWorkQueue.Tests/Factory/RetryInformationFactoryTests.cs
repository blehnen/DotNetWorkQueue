using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class RetryInformationFactoryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var t = typeof(NullReferenceException);
            var times = new List<TimeSpan> { TimeSpan.MinValue, TimeSpan.MaxValue };

            var factory = Create();
            var info = factory.Create(t, times);

            Assert.AreEqual(info.ExceptionType, t);
            Assert.AreEqual(info.Times, times);
            Assert.AreEqual(info.MaxRetries, times.Count);
        }
        [TestMethod]
        public void Create_Null_Params_Fails()
        {
            var factory = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
               delegate
               {
                   factory.Create(null, null);
               });
        }
        private IRetryInformationFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<RetryInformationFactory>();
        }
    }
}
