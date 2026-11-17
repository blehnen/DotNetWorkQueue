using System;
using System.Reflection;
using System.Threading.Tasks;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleInjector;

namespace DotNetWorkQueue.Tests.Shared.Tests
{
    [TestClass]
    public class HandlerRegistrationParityTests
    {
        private static readonly string[] FakePair = { "ICommandHandler<FakeCommand>" };
        private static readonly string[] SameNamePair = { "ICommandHandler<SameName>" };

        [TestMethod]
        public void Check_MatchingDecorators_ReportsThePairWithNoProblems()
        {
            var container = Container();
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RetryDecorator<>));
            container.RegisterDecorator(typeof(ICommandHandlerAsync<>), typeof(RetryDecoratorAsync<>));
            //"Async" before "Decorator" on one side and after it on the other still pairs up
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TraceDecorator<>));
            container.RegisterDecorator(typeof(ICommandHandlerAsync<>), typeof(TraceAsyncDecorator<>));

            var report = HandlerRegistrationParity.Check(container);

            Assert.IsEmpty(report.Problems, string.Join(Environment.NewLine, report.Problems));
            Assert.AreSequenceEqual(new[] { typeof(ICommandHandler<FakeCommand>) }, report.Pairs);
        }

        [TestMethod]
        public void Check_AsyncSideMissingADecorator_ReportsIt()
        {
            var container = Container();
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RetryDecorator<>));
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TraceDecorator<>));
            container.RegisterDecorator(typeof(ICommandHandlerAsync<>), typeof(RetryDecoratorAsync<>));

            var report = HandlerRegistrationParity.Check(container);

            Assert.HasCount(1, report.Problems);
            Assert.Contains("decorators differ", report.Problems[0]);
            Assert.Contains("TraceDecorator", report.Problems[0]);
        }

        [TestMethod]
        public void Check_DecoratorsInADifferentOrder_ReportsIt()
        {
            var container = Container();
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(RetryDecorator<>));
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(TraceDecorator<>));
            container.RegisterDecorator(typeof(ICommandHandlerAsync<>), typeof(TraceAsyncDecorator<>));
            container.RegisterDecorator(typeof(ICommandHandlerAsync<>), typeof(RetryDecoratorAsync<>));

            var report = HandlerRegistrationParity.Check(container);

            Assert.HasCount(1, report.Problems);
            Assert.Contains("decorators differ", report.Problems[0]);
        }

        [TestMethod]
        public void Check_DecorateeHeldByABaseClass_IsStillFollowed()
        {
            var container = Container();
            container.RegisterDecorator(typeof(ICommandHandler<>), typeof(DerivedRetryDecorator<>));

            var report = HandlerRegistrationParity.Check(container);

            //had the walk missed the field in the base class, the decorator would have been taken for
            //the handler, leaving no decorators on either side and nothing to report
            Assert.HasCount(1, report.Problems);
            Assert.Contains("DerivedRetryDecorator", report.Problems[0]);
        }

        [TestMethod]
        public void Check_AsyncHandlerFromAnotherAssembly_ReportsIt()
        {
            var container = new Container();
            container.Register<ICommandHandler<FakeCommand>, FakeHandler>();
            container.RegisterInstance(DispatchProxy.Create<ICommandHandlerAsync<FakeCommand>, CompletedProxy>());

            var report = HandlerRegistrationParity.Check(container);

            Assert.HasCount(1, report.Problems);
            Assert.Contains("comes from", report.Problems[0]);
        }

        [TestMethod]
        public void Check_OneSidedServices_AreListedNotCompared()
        {
            var container = new Container();
            container.Register<ICommandHandler<FakeCommand>, FakeHandler>();
            container.Register<ICommandHandlerAsync<OtherCommand>, OtherHandlerAsync>();

            var report = HandlerRegistrationParity.Check(container);

            Assert.IsEmpty(report.Pairs);
            Assert.IsEmpty(report.Problems);
            Assert.AreSequenceEqual(new[] { typeof(ICommandHandler<FakeCommand>) }, report.SyncOnly);
            Assert.AreSequenceEqual(new[] { typeof(ICommandHandlerAsync<OtherCommand>) }, report.AsyncOnly);
        }

        [TestMethod]
        public void AssertMatches_PairsAsExpected_Passes()
        {
            HandlerRegistrationParity.AssertMatches(Wrap(Container()), FakePair);
        }

        [TestMethod]
        public void AssertMatches_ExpectedPairMissing_Fails()
        {
            var container = new Container();
            container.Register<ICommandHandler<FakeCommand>, FakeHandler>();

            var error = Assert.ThrowsExactly<AssertFailedException>(() =>
                HandlerRegistrationParity.AssertMatches(Wrap(container), FakePair));
            Assert.Contains("no longer has an async twin", error.Message);
        }

        [TestMethod]
        public void AssertMatches_UnexpectedPair_Fails()
        {
            var error = Assert.ThrowsExactly<AssertFailedException>(() =>
                HandlerRegistrationParity.AssertMatches(Wrap(Container()), Array.Empty<string>()));
            Assert.Contains("add it to the expected list", error.Message);
        }

        [TestMethod]
        public void AssertMatches_TwoPairsWithTheSameShortName_Fails()
        {
            var container = new Container();
            container.Register<ICommandHandler<First.SameName>, FirstHandler>();
            container.Register<ICommandHandlerAsync<First.SameName>, FirstHandlerAsync>();
            container.Register<ICommandHandler<Second.SameName>, SecondHandler>();
            container.Register<ICommandHandlerAsync<Second.SameName>, SecondHandlerAsync>();

            //one name in the list would otherwise stand for both pairs, so losing either would go unnoticed
            var error = Assert.ThrowsExactly<AssertFailedException>(() =>
                HandlerRegistrationParity.AssertMatches(Wrap(container), SameNamePair));
            Assert.Contains("2 pairs share this name", error.Message);
        }

        [TestMethod]
        public void Name_ClosedGeneric_IsReadable()
        {
            Assert.AreEqual("ICommandHandlerWithOutput<FakeCommand, Int64>",
                HandlerRegistrationParity.Name(typeof(ICommandHandlerWithOutput<FakeCommand, long>)));
        }

        private static Container Container()
        {
            var container = new Container();
            container.Register<ICommandHandler<FakeCommand>, FakeHandler>();
            container.Register<ICommandHandlerAsync<FakeCommand>, FakeHandlerAsync>();
            return container;
        }

        private static IContainer Wrap(Container container) => new ContainerWrapper(container);

        public static class First { public sealed class SameName { } }
        public static class Second { public sealed class SameName { } }

        public sealed class FirstHandler : ICommandHandler<First.SameName> { public void Handle(First.SameName command) { } }
        public sealed class FirstHandlerAsync : ICommandHandlerAsync<First.SameName> { public Task HandleAsync(First.SameName command) => Task.CompletedTask; }
        public sealed class SecondHandler : ICommandHandler<Second.SameName> { public void Handle(Second.SameName command) { } }
        public sealed class SecondHandlerAsync : ICommandHandlerAsync<Second.SameName> { public Task HandleAsync(Second.SameName command) => Task.CompletedTask; }

        public sealed class FakeCommand { }
        public sealed class OtherCommand { }

        public sealed class FakeHandler : ICommandHandler<FakeCommand>
        {
            public void Handle(FakeCommand command) { }
        }

        public sealed class FakeHandlerAsync : ICommandHandlerAsync<FakeCommand>
        {
            public Task HandleAsync(FakeCommand command) => Task.CompletedTask;
        }

        public sealed class OtherHandlerAsync : ICommandHandlerAsync<OtherCommand>
        {
            public Task HandleAsync(OtherCommand command) => Task.CompletedTask;
        }

        public sealed class RetryDecorator<T> : ICommandHandler<T>
        {
            private readonly ICommandHandler<T> _decorated;
            public RetryDecorator(ICommandHandler<T> decorated) => _decorated = decorated;
            public void Handle(T command) => _decorated.Handle(command);
        }

        public sealed class RetryDecoratorAsync<T> : ICommandHandlerAsync<T>
        {
            private readonly ICommandHandlerAsync<T> _decorated;
            public RetryDecoratorAsync(ICommandHandlerAsync<T> decorated) => _decorated = decorated;
            public Task HandleAsync(T command) => _decorated.HandleAsync(command);
        }

        public sealed class TraceDecorator<T> : ICommandHandler<T>
        {
            private readonly ICommandHandler<T> _decorated;
            public TraceDecorator(ICommandHandler<T> decorated) => _decorated = decorated;
            public void Handle(T command) => _decorated.Handle(command);
        }

        public sealed class TraceAsyncDecorator<T> : ICommandHandlerAsync<T>
        {
            private readonly ICommandHandlerAsync<T> _decorated;
            public TraceAsyncDecorator(ICommandHandlerAsync<T> decorated) => _decorated = decorated;
            public Task HandleAsync(T command) => _decorated.HandleAsync(command);
        }

        public abstract class RetryDecoratorBase<T> : ICommandHandler<T>
        {
            private readonly ICommandHandler<T> _decorated;
            protected RetryDecoratorBase(ICommandHandler<T> decorated) => _decorated = decorated;
            public void Handle(T command) => _decorated.Handle(command);
        }

        public sealed class DerivedRetryDecorator<T> : RetryDecoratorBase<T>
        {
            public DerivedRetryDecorator(ICommandHandler<T> decorated) : base(decorated) { }
        }

        public class CompletedProxy : DispatchProxy
        {
            protected override object Invoke(MethodInfo targetMethod, object[] args) => Task.CompletedTask;
        }
    }
}
