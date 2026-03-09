using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.AppMetrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App.Metrics;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    [TestClass]
    public class ExtensionMethodsTests
    {
        [TestMethod]
        public void GetCurrentMetrics_Test()
        {
            var test = Create();
            var result = test.GetCurrentMetrics();
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetCurrentMetrics_Test1()
        {
            var test = CreateMetrics();
            var result = test.GetCurrentMetrics();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetTags_Test()
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
            var result = data.GetTags();
            Assert.IsTrue(result.Count == 0);

            data.Add(new KeyValuePair<string, string>("1", "2"));
            result = data.GetTags();

            Assert.AreEqual("1", result.Keys[0]);
            Assert.AreEqual("2", result.Values[0]);
        }

        [TestMethod]
        public void GetUnit_Test()
        {
            var result = Units.Bytes.GetUnit();
            Assert.AreEqual(Unit.Bytes, result);

            result = Units.Calls.GetUnit();
            Assert.AreEqual(Unit.Calls, result);

            result = Units.Commands.GetUnit();
            Assert.AreEqual(Unit.Commands, result);

            result = Units.Errors.GetUnit();
            Assert.AreEqual(Unit.Errors, result);

            result = Units.Events.GetUnit();
            Assert.AreEqual(Unit.Events, result);

            result = Units.Items.GetUnit();
            Assert.AreEqual(Unit.Items, result);

            result = Units.KiloBytes.GetUnit();
            Assert.AreEqual(Unit.KiloBytes, result);

            result = Units.MegaBytes.GetUnit();
            Assert.AreEqual(Unit.MegaBytes, result);

            result = Units.None.GetUnit();
            Assert.AreEqual(Unit.None, result);

            result = Units.Percent.GetUnit();
            Assert.AreEqual(Unit.Percent, result);

            result = Units.Requests.GetUnit();
            Assert.AreEqual(Unit.Requests, result);

            result = Units.Results.GetUnit();
            Assert.AreEqual(Unit.Results, result);

            result = Units.Threads.GetUnit();
            Assert.AreEqual(Unit.Threads, result);
        }

        private App.Metrics.IMetrics Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<App.Metrics.IMetrics>();
        }

        private DotNetWorkQueue.AppMetrics.Metrics CreateMetrics()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<DotNetWorkQueue.AppMetrics.Metrics>();
        }
    }
}