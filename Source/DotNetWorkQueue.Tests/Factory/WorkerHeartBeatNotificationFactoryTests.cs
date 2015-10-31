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

using System.Threading;
using DotNetWorkQueue.Factory;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Factory
{
    public class WorkerHeartBeatNotificationFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create(true);
            using (var source = new CancellationTokenSource())
            {
                Assert.NotNull(factory.Create(source.Token));
            }
        }

        [Fact]
        public void Create_NoOp()
        {
            var factory = Create(false);
            using (var source = new CancellationTokenSource())
            {
                var test = factory.Create(source.Token);
                Assert.IsAssignableFrom<INoOperation>(test);
            }
        }

        private IWorkerHeartBeatNotificationFactory Create(bool enabled)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<IHeartBeatConfiguration>();
            configuration.Enabled.Returns(enabled);
            fixture.Inject(configuration);
            return fixture.Create<WorkerHeartBeatNotificationFactory>();
        }
    }
}
