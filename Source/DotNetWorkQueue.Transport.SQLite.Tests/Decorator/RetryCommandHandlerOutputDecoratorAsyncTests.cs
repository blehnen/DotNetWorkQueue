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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Polly.Registry;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Decorator
{
    [TestClass]
    public class RetryCommandHandlerOutputDecoratorAsyncTests
    {
        public sealed class FakeCommand { }

        [TestMethod]
        public async Task HandleAsync_WhenRegistryDisposed_FallsThroughToDecorated()
        {
            var decorated = Substitute.For<ICommandHandlerWithOutputAsync<FakeCommand, int>>();
            decorated.HandleAsync(Arg.Any<FakeCommand>()).Returns(Task.FromResult(42));
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            registry.Dispose();
            policies.Registry.Returns(registry);

            var sut = new RetryCommandHandlerOutputDecoratorAsync<FakeCommand, int>(decorated, policies);
            var cmd = new FakeCommand();

            var result = await sut.HandleAsync(cmd);

            Assert.AreEqual(42, result);
            await decorated.Received(1).HandleAsync(cmd);
        }

        [TestMethod]
        public async Task HandleAsync_WhenPipelineRegistered_ExecutesThroughPipeline()
        {
            var decorated = Substitute.For<ICommandHandlerWithOutputAsync<FakeCommand, int>>();
            decorated.HandleAsync(Arg.Any<FakeCommand>()).Returns(Task.FromResult(42));
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(TransportPolicyDefinitions.RetryCommandHandlerAsync, (_, _) => { });
            policies.Registry.Returns(registry);

            var sut = new RetryCommandHandlerOutputDecoratorAsync<FakeCommand, int>(decorated, policies);
            var cmd = new FakeCommand();

            var result = await sut.HandleAsync(cmd);

            Assert.AreEqual(42, result);
            await decorated.Received(1).HandleAsync(cmd);
            registry.Dispose();
        }

        [TestMethod]
        public async Task HandleAsync_WhenNoPipelineRegistered_CallsDecoratedDirectly()
        {
            var decorated = Substitute.For<ICommandHandlerWithOutputAsync<FakeCommand, int>>();
            decorated.HandleAsync(Arg.Any<FakeCommand>()).Returns(Task.FromResult(42));
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            policies.Registry.Returns(registry);

            var sut = new RetryCommandHandlerOutputDecoratorAsync<FakeCommand, int>(decorated, policies);
            var cmd = new FakeCommand();

            var result = await sut.HandleAsync(cmd);

            Assert.AreEqual(42, result);
            await decorated.Received(1).HandleAsync(cmd);
            registry.Dispose();
        }
    }
}
