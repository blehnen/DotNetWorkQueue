using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.Memory.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    [TestClass]
    public class CreationScopeTests
    {
        [TestMethod]
        public void AddScopedObject_Test()
        {
            using (var scope = Create())
            {
                scope.AddScopedObject(CreateClear());
                Assert.IsTrue(scope.ContainedClears.Count == 1);
                scope.AddScopedObject(CreateClear());
                Assert.IsTrue(scope.ContainedClears.Count == 2);
            }
        }

        [TestMethod]
        public void AddScopedObject_Test1()
        {
            using (var scope = Create())
            {
                scope.AddScopedObject(createDisposable());
                Assert.IsTrue(scope.ContainedDisposables.Count == 1);
                scope.AddScopedObject(createDisposable());
                Assert.IsTrue(scope.ContainedDisposables.Count == 2);
            }
        }

        [TestMethod]
        public void Dispose_Test()
        {
            var scope = Create();
            var clear = CreateClear();
            var clear2 = CreateClear();
            var dispose = createDisposable();
            var dispose2 = createDisposable();
            var dispose3 = createDisposable();

            scope.AddScopedObject(clear);
            scope.AddScopedObject(clear2);
            scope.AddScopedObject(dispose);
            scope.AddScopedObject(dispose2);
            scope.AddScopedObject(dispose3);
            scope.Dispose();

            Assert.IsEmpty(scope.ContainedClears);
            Assert.IsEmpty(scope.ContainedDisposables);

            clear.Received().Clear();
            clear2.Received().Clear();

            dispose.Received().Dispose();
            dispose2.Received().Dispose();
            dispose3.Received().Dispose();
        }

        private CreationScope Create()
        {
            return new CreationScope();
        }

        private IDisposable createDisposable()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<IDisposable>();
        }
        private IClear CreateClear()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<IClear>();
        }
    }
}