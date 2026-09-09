// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Decorator;
using DotNetWorkQueue.Transport.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Polly.Registry;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Decorator
{
    /// <summary>
    /// The no-output async retry decorator. Mirrors the with-output tests beside it; the two differ
    /// only in whether the handler returns a value.
    /// </summary>
    [TestClass]
    public class RetryCommandHandlerDecoratorAsyncTests
    {
        public sealed class FakeCommand { }

        [TestMethod]
        public async Task HandleAsync_WhenRegistryDisposed_FallsThroughToDecorated()
        {
            var decorated = Substitute.For<ICommandHandlerAsync<FakeCommand>>();
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            registry.Dispose();
            policies.Registry.Returns(registry);

            var sut = new RetryCommandHandlerDecoratorAsync<FakeCommand>(decorated, policies);
            var cmd = new FakeCommand();

            await sut.HandleAsync(cmd);

            await decorated.Received(1).HandleAsync(cmd);
        }

        [TestMethod]
        public async Task HandleAsync_WhenPipelineRegistered_ExecutesThroughPipeline()
        {
            var decorated = Substitute.For<ICommandHandlerAsync<FakeCommand>>();
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            registry.GetOrAddPipeline(TransportPolicyDefinitions.RetryCommandHandlerAsync,
                builder => builder.AddTimeout(System.TimeSpan.FromSeconds(30)));
            policies.Registry.Returns(registry);

            var sut = new RetryCommandHandlerDecoratorAsync<FakeCommand>(decorated, policies);
            var cmd = new FakeCommand();

            await sut.HandleAsync(cmd);

            await decorated.Received(1).HandleAsync(cmd);
        }

        [TestMethod]
        public async Task HandleAsync_WhenNoPipelineRegistered_CallsDecoratedDirectly()
        {
            var decorated = Substitute.For<ICommandHandlerAsync<FakeCommand>>();
            var policies = Substitute.For<IPolicies>();
            policies.Registry.Returns(new ResiliencePipelineRegistry<string>());

            var sut = new RetryCommandHandlerDecoratorAsync<FakeCommand>(decorated, policies);
            var cmd = new FakeCommand();

            await sut.HandleAsync(cmd);

            await decorated.Received(1).HandleAsync(cmd);
        }
    }
}
