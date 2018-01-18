using System;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.TaskScheduling;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using Xunit;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class SchedulerInitTests
    {
        [Fact]
        public void CreateContainer_NoWarnings_SchedulerInitTransport()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var creator = new CreateContainer<SchedulerInit>();
            var c = creator.Create(QueueContexts.TaskScheduler, x => { }, fixture.Create<SchedulerInit>(), y => { });

            // Assert
            Container container = c.Container;
            var results = Analyzer.Analyze(container);
            Assert.False(results.Any(), Environment.NewLine +
                                        string.Join(Environment.NewLine,
                                            from result in results
                                            select result.Description));
        }
    }
}
