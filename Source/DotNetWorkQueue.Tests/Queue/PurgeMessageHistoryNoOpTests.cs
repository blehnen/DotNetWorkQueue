using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class PurgeMessageHistoryNoOpTests
    {
        [TestMethod]
        public void Purge_Returns_Zero()
        {
            var test = Create();
            var result = test.Purge(DateTime.UtcNow);
            Assert.AreEqual(0, result);
        }

        private IPurgeMessageHistory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<PurgeMessageHistoryNoOp>();
        }
    }
}
