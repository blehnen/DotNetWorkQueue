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
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerTests
    {
        [Fact]
        public void Start_Stop()
        {
            using (var worker = Create())
            {
                worker.Start();
                worker.Stop();
            }
        }

        [Fact]
        public void Start_Stop_Start()
        {
            using (var worker = Create())
            {
                worker.Start();
                worker.Stop();
                worker.Start();
            }
        }

        [Fact]
        public void Stop_without_Start_Ok()
        {
            using (var worker = Create())
            {
                worker.Stop();
            }
        }

        [Fact]
        public void Stop_Multiple_Ok()
        {
            using (var worker = Create())
            {
                worker.Stop();
                worker.Stop();
            }
        }

        [Fact]
        public void Start_Multiple_Ok()
        {
            using (var worker = Create())
            {
                worker.Start();
                worker.Start();
            }
        }

        private IWorker Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var container = fixture.Create<IContainer>();
            fixture.Inject(container);
            var fact = fixture.Create<MessageProcessingFactory>();
            var wrapper = new MessageProcessingTests.MessageProcessingWrapper();
            var processing = wrapper.Create();
            fact.Create().Returns(processing);
            fixture.Inject(fact);
            return fixture.Create<Worker>();
        }
    }
}
