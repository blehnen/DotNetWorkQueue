using System;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.TaskScheduling;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class SchedulerInitTests
    {
        [TestMethod]
        public void CreateContainer_NoWarnings_SchedulerInitTransport()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var creator = new CreateContainer<SchedulerInit>();
            var c = creator.Create(QueueContexts.TaskScheduler, x => { }, fixture.Create<SchedulerInit>(), y => { });

            // Assert
            Container container = c.Container;
            var results = Analyzer.Analyze(container);
            Assert.IsFalse(results.Any(), Environment.NewLine +
                                        string.Join(Environment.NewLine,
                                            from result in results
                                            select result.Description));
        }
    }
}
