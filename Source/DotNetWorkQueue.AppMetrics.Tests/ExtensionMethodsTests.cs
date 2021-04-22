using Xunit;
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
    public class ExtensionMethodsTests
    {
        [Fact()]
        public void GetCurrentMetrics_Test()
        {
            var test = Create();
            var result = test.GetCurrentMetrics();
            Assert.Null(result);
        }

        [Fact()]
        public void GetCurrentMetrics_Test1()
        {
            var test = CreateMetrics();
            var result = test.GetCurrentMetrics();
            Assert.NotNull(result);
        }

        [Fact()]
        public void GetTags_Test()
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
            var result = data.GetTags();
            Assert.True(result.Count == 0);

            data.Add(new KeyValuePair<string, string>("1", "2"));
            result = data.GetTags();

            Assert.Equal("1", result.Keys[0]);
            Assert.Equal("2", result.Values[0]);
        }

        [Fact()]
        public void GetUnit_Test()
        {
            var result = Units.Bytes.GetUnit();
            Assert.Equal(Unit.Bytes, result);

            result = Units.Calls.GetUnit();
            Assert.Equal(Unit.Calls, result);

            result = Units.Commands.GetUnit();
            Assert.Equal(Unit.Commands, result);

            result = Units.Errors.GetUnit();
            Assert.Equal(Unit.Errors, result);

            result = Units.Events.GetUnit();
            Assert.Equal(Unit.Events, result);

            result = Units.Items.GetUnit();
            Assert.Equal(Unit.Items, result);

            result = Units.KiloBytes.GetUnit();
            Assert.Equal(Unit.KiloBytes, result);

            result = Units.MegaBytes.GetUnit();
            Assert.Equal(Unit.MegaBytes, result);

            result = Units.None.GetUnit();
            Assert.Equal(Unit.None, result);

            result = Units.Percent.GetUnit();
            Assert.Equal(Unit.Percent, result);

            result = Units.Requests.GetUnit();
            Assert.Equal(Unit.Requests, result);

            result = Units.Results.GetUnit();
            Assert.Equal(Unit.Results, result);

            result = Units.Threads.GetUnit();
            Assert.Equal(Unit.Threads, result);
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