// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System;
using System.Linq;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.TaskScheduling;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
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
