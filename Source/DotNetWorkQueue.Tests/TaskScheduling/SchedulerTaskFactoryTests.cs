// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Threading.Tasks;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class SchedulerTaskFactoryTests
    {
        [Fact]
        public void GetSet_Scheduler()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var scheduler = fixture.Create<ATaskScheduler>();
            var test = Create(scheduler);
            Assert.Equal(scheduler, test.Scheduler);
        }

        [Fact]
        public void TryStartNew_Null_Action_Exception()
        {
            var test = Create();

            Task temp;
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    test.TryStartNew(null, new StateInformation(Substitute.For<IWorkGroup>()), x => { },
                        out temp);
                });
        }

        [Fact]
        public void TryStartNew()
        {
            var test = Create();
            Task temp;

            test.TryStartNew(x => { }, new StateInformation(Substitute.For<IWorkGroup>()), x => { },
                out temp);
        }

        private SchedulerTaskFactory Create(ATaskScheduler scheduler)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(scheduler);
            var factory = fixture.Create<ITaskSchedulerFactory>();
            factory.Create().Returns(scheduler);
            fixture.Inject(factory);
            return fixture.Create<SchedulerTaskFactory>();
        }

        private SchedulerTaskFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var scheduler = fixture.Create<ATaskScheduler>();
            return Create(scheduler);
        }
    }
}
