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
using System.Data;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly.Registry;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Decorator
{
    [TestClass]
    public class BeginTransactionRetryDecoratorTests
    {
        [TestMethod]
        public void BeginTransaction_WhenRegistryDisposed_FallsThroughToDecorated()
        {
            var decoratedTxn = Substitute.For<IDbTransaction>();
            var decorated = Substitute.For<ISQLiteTransactionWrapper>();
            decorated.BeginTransaction().Returns(decoratedTxn);
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            registry.Dispose();
            policies.Registry.Returns(registry);

            var sut = new BeginTransactionRetryDecorator(decorated, policies);

            var result = sut.BeginTransaction();

            Assert.AreSame(decoratedTxn, result);
            decorated.Received(1).BeginTransaction();
        }

        [TestMethod]
        public void BeginTransaction_WhenPipelineRegistered_ExecutesThroughPipeline()
        {
            var decoratedTxn = Substitute.For<IDbTransaction>();
            var decorated = Substitute.For<ISQLiteTransactionWrapper>();
            decorated.BeginTransaction().Returns(decoratedTxn);
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(TransportPolicyDefinitions.BeginTransaction, (_, _) => { });
            policies.Registry.Returns(registry);

            var sut = new BeginTransactionRetryDecorator(decorated, policies);

            var result = sut.BeginTransaction();

            Assert.AreSame(decoratedTxn, result);
            decorated.Received(1).BeginTransaction();
            registry.Dispose();
        }

        [TestMethod]
        public void BeginTransaction_WhenNoPipelineRegistered_CallsDecoratedDirectly()
        {
            var decoratedTxn = Substitute.For<IDbTransaction>();
            var decorated = Substitute.For<ISQLiteTransactionWrapper>();
            decorated.BeginTransaction().Returns(decoratedTxn);
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            policies.Registry.Returns(registry);

            var sut = new BeginTransactionRetryDecorator(decorated, policies);

            var result = sut.BeginTransaction();

            Assert.AreSame(decoratedTxn, result);
            decorated.Received(1).BeginTransaction();
            registry.Dispose();
        }
    }
}
