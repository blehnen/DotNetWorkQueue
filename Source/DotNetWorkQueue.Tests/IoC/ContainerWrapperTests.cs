using System;
using System.Collections.Generic;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.IoC;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleInjector;

namespace DotNetWorkQueue.Tests.IoC
{
    [TestClass]
    public class ContainerWrapperTests
    {
        [TestMethod]
        public void Create_Default()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                Assert.IsNotNull(wrapper);
            }
        }

        [TestMethod]
        public void Create_Null_Container_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new ContainerWrapper(null));
        }

        [TestMethod]
        public void IsDisposed_False_Before_Dispose()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.IsDisposed.Should().BeFalse();
            }
        }

        [TestMethod]
        public void IsDisposed_True_After_Dispose()
        {
            var wrapper = new ContainerWrapper(new Container());
            wrapper.Dispose();
            wrapper.IsDisposed.Should().BeTrue();
        }

        [TestMethod]
        public void Dispose_Idempotent()
        {
            var wrapper = new ContainerWrapper(new Container());
            wrapper.Dispose();
            wrapper.Dispose(); // Second call should not throw
            wrapper.IsDisposed.Should().BeTrue();
        }

        [TestMethod]
        public void Container_Property_Returns_Inner_Container()
        {
            var container = new Container();
            using (var wrapper = new ContainerWrapper(container))
            {
                object inner = wrapper.Container;
                inner.Should().BeSameAs(container);
            }
        }

        [TestMethod]
        public void IsVerifying_Returns_False_When_Not_Verifying()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.IsVerifying.Should().BeFalse();
            }
        }

        [TestMethod]
        public void TypesThatCanBeSuppressed_Empty_Initially()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.TypesThatCanBeSuppressed.Should().BeEmpty();
            }
        }

        // === Registration methods ===

        [TestMethod]
        public void Register_Generic_ServiceAndImpl_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.Register<ITestService, TestServiceImpl>(LifeStyles.Transient);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void Register_Generic_ServiceAndImpl_Resolves()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.Register<ITestService, TestServiceImpl>(LifeStyles.Transient);
                var instance = wrapper.GetInstance<ITestService>();
                instance.Should().BeOfType<TestServiceImpl>();
            }
        }

        [TestMethod]
        public void Register_Concrete_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.Register<TestServiceImpl>(LifeStyles.Transient);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void Register_InstanceCreator_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.Register<ITestService>(() => new TestServiceImpl(), LifeStyles.Transient);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void Register_InstanceCreator_Resolves()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.Register<ITestService>(() => new TestServiceImpl(), LifeStyles.Transient);
                var instance = wrapper.GetInstance<ITestService>();
                instance.Should().BeOfType<TestServiceImpl>();
            }
        }

        [TestMethod]
        public void Register_TypeServiceType_InstanceCreator_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.Register(typeof(ITestService), () => new TestServiceImpl(), LifeStyles.Transient);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void Register_ServiceType_ImplementationType_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.Register(typeof(ITestService), typeof(TestServiceImpl), LifeStyles.Transient);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void Register_ServiceType_ImplementationType_Resolves()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.Register(typeof(ITestService), typeof(TestServiceImpl), LifeStyles.Transient);
                var instance = wrapper.GetInstance(typeof(ITestService));
                instance.Should().BeOfType<TestServiceImpl>();
            }
        }

        [TestMethod]
        public void Register_OpenGeneric_ImplementationTypes_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.Register(typeof(IGenericService<>),
                    new[] { typeof(StringServiceImpl) }, LifeStyles.Transient);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void RegisterConditional_Type_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.RegisterConditional(typeof(ITestService), typeof(TestServiceImpl),
                    LifeStyles.Transient);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void RegisterConditional_Generic_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.RegisterConditional<ITestService, TestServiceImpl>(LifeStyles.Transient);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void RegisterNonScopedSingleton_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var instance = new TestServiceImpl();
                var result = wrapper.RegisterNonScopedSingleton(instance);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void RegisterNonScopedSingleton_Resolves_Same_Instance()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var instance = new TestServiceImpl();
                wrapper.RegisterNonScopedSingleton(instance);
                var resolved = wrapper.GetInstance<TestServiceImpl>();
                resolved.Should().BeSameAs(instance);
            }
        }

        [TestMethod]
        public void RegisterCollection_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.RegisterCollection<ITestService>(new[] { typeof(TestServiceImpl) });
                result.Should().BeSameAs(wrapper);
            }
        }

        // === Singleton lifestyle ===

        [TestMethod]
        public void Register_Singleton_Returns_Same_Instance()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.Register<ITestService, TestServiceImpl>(LifeStyles.Singleton);
                var instance1 = wrapper.GetInstance<ITestService>();
                var instance2 = wrapper.GetInstance<ITestService>();
                instance1.Should().BeSameAs(instance2);
            }
        }

        [TestMethod]
        public void Register_Transient_Returns_Different_Instances()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.Register<ITestService, TestServiceImpl>(LifeStyles.Transient);
                var instance1 = wrapper.GetInstance<ITestService>();
                var instance2 = wrapper.GetInstance<ITestService>();
                instance1.Should().NotBeSameAs(instance2);
            }
        }

        // === Invalid lifestyle ===

        [TestMethod]
        public void Register_Invalid_Lifestyle_Throws()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                Action act = () => wrapper.Register<ITestService, TestServiceImpl>((LifeStyles)99);
                act.Should().Throw<DotNetWorkQueueException>();
            }
        }

        // === Suppress diagnostic warnings ===

        [TestMethod]
        public void SuppressDiagnosticWarning_Null_Type_Throws()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                Action act = () => wrapper.SuppressDiagnosticWarning(null,
                    DiagnosticTypes.DisposableTransientComponent, "test");
                act.Should().Throw<ArgumentNullException>();
            }
        }

        [TestMethod]
        public void SuppressDiagnosticWarning_Type_Not_In_Set_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.SuppressDiagnosticWarning(typeof(ITestService),
                    DiagnosticTypes.DisposableTransientComponent, "test");
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void SuppressDiagnosticWarning_Type_In_Set_But_Not_Registered_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.AddTypeThatNeedsWarningSuppression(typeof(ITestService));
                var result = wrapper.SuppressDiagnosticWarning(typeof(ITestService),
                    DiagnosticTypes.DisposableTransientComponent, "test");
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void SuppressDiagnosticWarning_Type_Registered_DoesNotThrow()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.Register<ITestService, TestServiceImpl>(LifeStyles.Transient);
                wrapper.AddTypeThatNeedsWarningSuppression(typeof(ITestService));

                Action act = () => wrapper.SuppressDiagnosticWarning(typeof(ITestService),
                    DiagnosticTypes.DisposableTransientComponent, "test reason");
                act.Should().NotThrow();
            }
        }

        // === AddTypeThatNeedsWarningSuppression ===

        [TestMethod]
        public void AddTypeThatNeedsWarningSuppression_Adds_Type()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.AddTypeThatNeedsWarningSuppression(typeof(ITestService));
                wrapper.TypesThatCanBeSuppressed.Should().Contain(typeof(ITestService));
            }
        }

        [TestMethod]
        public void AddTypeThatNeedsWarningSuppression_Duplicate_DoesNotThrow()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.AddTypeThatNeedsWarningSuppression(typeof(ITestService));
                wrapper.AddTypeThatNeedsWarningSuppression(typeof(ITestService));
                wrapper.TypesThatCanBeSuppressed.Should().HaveCount(1);
            }
        }

        // === Decorator registration ===

        [TestMethod]
        public void RegisterDecorator_Type_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.Register<ITestService, TestServiceImpl>(LifeStyles.Transient);
                var result = wrapper.RegisterDecorator(typeof(ITestService), typeof(TestServiceDecorator),
                    LifeStyles.Transient);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void RegisterDecorator_Generic_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.Register<ITestService, TestServiceImpl>(LifeStyles.Transient);
                var result = wrapper.RegisterDecorator<ITestService, TestServiceDecorator>(LifeStyles.Transient);
                result.Should().BeSameAs(wrapper);
            }
        }

        [TestMethod]
        public void RegisterDecorator_Resolves_Decorated_Instance()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                wrapper.Register<ITestService, TestServiceImpl>(LifeStyles.Transient);
                wrapper.RegisterDecorator<ITestService, TestServiceDecorator>(LifeStyles.Transient);

                var instance = wrapper.GetInstance<ITestService>();
                instance.Should().BeOfType<TestServiceDecorator>();
            }
        }

        // === Register with Assembly ===

        [TestMethod]
        public void Register_OpenGenericServiceType_With_Assembly_Returns_Self()
        {
            using (var wrapper = new ContainerWrapper(new Container()))
            {
                var result = wrapper.Register(typeof(IGenericService<>), LifeStyles.Transient,
                    typeof(GenericServiceImpl<>).Assembly);
                result.Should().BeSameAs(wrapper);
            }
        }

        // === Test types ===

        public interface ITestService
        {
        }

        public class TestServiceImpl : ITestService
        {
        }

        public class TestServiceDecorator : ITestService
        {
            public TestServiceDecorator(ITestService inner)
            {
            }
        }

        public interface IGenericService<T>
        {
        }

        public class GenericServiceImpl<T> : IGenericService<T>
        {
        }

        public class StringServiceImpl : IGenericService<string>
        {
        }
    }
}
