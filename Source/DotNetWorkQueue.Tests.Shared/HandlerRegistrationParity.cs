using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetWorkQueue.Transport.Shared;
using SimpleInjector;

namespace DotNetWorkQueue.Tests.Shared
{
    /// <summary>
    /// Compares each sync command/query handler registration with its async twin.
    /// </summary>
    /// <remarks>
    /// A missing half of a pair still compiles and still resolves: a shared layer's handler answers
    /// instead (#301), or a decorator is simply absent (#297). For every sync service with an async
    /// twin, the two must resolve to handlers from the same assembly, wrapped in the same decorators.
    /// Services registered on one side only are listed, so a test can pin the ones that are one-sided
    /// by design and fail on any new one.
    /// </remarks>
    public static class HandlerRegistrationParity
    {
        private static readonly (Type Sync, Type Async)[] Families =
        {
            (typeof(ICommandHandler<>), typeof(ICommandHandlerAsync<>)),
            (typeof(ICommandHandlerWithOutput<,>), typeof(ICommandHandlerWithOutputAsync<,>)),
            (typeof(IQueryHandler<,>), typeof(IQueryHandlerAsync<,>)),
        };

        /// <summary>
        /// Asserts that every pair matches, and that the pairs are exactly <paramref name="expectedPairs"/>.
        /// </summary>
        /// <remarks>
        /// Pinning the pairs is what catches a deleted async registration that nothing else answers:
        /// without it the pair would quietly become sync-only.
        /// </remarks>
        /// <param name="container">A container built by a transport.</param>
        /// <param name="expectedPairs">The sync service of each pair, as <see cref="Name"/> writes it.</param>
        public static void AssertMatches(IContainer container, IEnumerable<string> expectedPairs)
        {
            var report = Check(container);
            var actual = new HashSet<string>(report.Pairs.Select(Name));
            var expected = new HashSet<string>(expectedPairs);

            var failures = new List<string>(report.Problems);
            failures.AddRange(expected.Where(e => !actual.Contains(e)).OrderBy(e => e)
                .Select(e => $"{e}: no longer has an async twin, or is no longer registered"));
            failures.AddRange(actual.Where(a => !expected.Contains(a)).OrderBy(a => a)
                .Select(a => $"{a}: a new sync/async pair; add it to the expected list"));

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsEmpty(failures,
                Environment.NewLine + string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Checks every handler registration in the container.
        /// </summary>
        /// <param name="container">A container built by a transport.</param>
        public static ParityReport Check(IContainer container)
        {
            return Check((Container)container.Container);
        }

        /// <summary>
        /// Checks every handler registration in the container.
        /// </summary>
        /// <param name="container">The SimpleInjector container.</param>
        public static ParityReport Check(Container container)
        {
            var registered = new HashSet<Type>(container.GetCurrentRegistrations()
                .Select(p => p.ServiceType)
                .Where(t => t.IsGenericType && !t.ContainsGenericParameters));

            var report = new ParityReport();
            foreach (var (syncDefinition, asyncDefinition) in Families)
            {
                var syncTypes = registered.Where(t => t.GetGenericTypeDefinition() == syncDefinition).ToList();
                var asyncTypes = new HashSet<Type>(registered.Where(t => t.GetGenericTypeDefinition() == asyncDefinition));

                foreach (var syncType in syncTypes)
                {
                    var asyncType = asyncDefinition.MakeGenericType(syncType.GetGenericArguments());
                    if (!asyncTypes.Remove(asyncType))
                    {
                        report.SyncOnly.Add(syncType);
                        continue;
                    }

                    report.Pairs.Add(syncType);
                    Compare(container, syncType, asyncType, report.Problems);
                }

                report.AsyncOnly.AddRange(asyncTypes);
            }

            return report;
        }

        private static void Compare(Container container, Type syncType, Type asyncType, List<string> problems)
        {
            var syncChain = Chain(container.GetInstance(syncType), syncType);
            var asyncChain = Chain(container.GetInstance(asyncType), asyncType);

            var syncHandler = syncChain[syncChain.Count - 1];
            var asyncHandler = asyncChain[asyncChain.Count - 1];
            if (syncHandler.Assembly != asyncHandler.Assembly)
            {
                problems.Add($"{Name(syncType)}: the sync handler {Name(syncHandler)} comes from " +
                             $"{syncHandler.Assembly.GetName().Name}, but the async handler {Name(asyncHandler)} " +
                             $"comes from {asyncHandler.Assembly.GetName().Name}");
            }

            var syncDecorators = syncChain.Take(syncChain.Count - 1).Select(Family).ToList();
            var asyncDecorators = asyncChain.Take(asyncChain.Count - 1).Select(Family).ToList();
            if (!syncDecorators.SequenceEqual(asyncDecorators))
            {
                problems.Add($"{Name(syncType)}: the decorators differ. " +
                             $"Sync: [{string.Join(", ", syncChain.Take(syncChain.Count - 1).Select(Name))}]. " +
                             $"Async: [{string.Join(", ", asyncChain.Take(asyncChain.Count - 1).Select(Name))}]");
            }
        }

        /// <summary>
        /// The types from the outermost decorator to the handler. Each decorator holds the instance it
        /// wraps in a field of the service type.
        /// </summary>
        private static List<Type> Chain(object instance, Type serviceType)
        {
            var chain = new List<Type>();
            var current = instance;
            while (current != null)
            {
                chain.Add(current.GetType());
                if (chain.Count > 32)
                    throw new InvalidOperationException($"{Name(serviceType)}: the decorator chain does not end");
                current = Decoratee(current, serviceType);
            }

            return chain;
        }

        private static object Decoratee(object instance, Type serviceType)
        {
            for (var type = instance.GetType(); type != null; type = type.BaseType)
            {
                var field = type
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(f => f.FieldType == serviceType);
                if (field != null)
                    return field.GetValue(instance);
            }

            return null;
        }

        /// <summary>
        /// A decorator's name with "Async" removed, so that a sync decorator and its async twin match
        /// whichever way round the suffix is placed (RetryDecoratorAsync, SendAsyncDecorator).
        /// </summary>
        private static string Family(Type decorator)
        {
            var name = decorator.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0)
                name = name.Substring(0, tick);
            return decorator.Namespace + "." + name.Replace("Async", string.Empty);
        }

        /// <summary>
        /// A readable name for a closed generic type, such as ICommandHandler&lt;SendMessageCommand&gt;.
        /// </summary>
        public static string Name(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
            var name = type.Name.Substring(0, type.Name.IndexOf('`'));
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Name))}>";
        }
    }

    /// <summary>
    /// The result of <see cref="HandlerRegistrationParity.Check(IContainer)"/>.
    /// </summary>
    public sealed class ParityReport
    {
        /// <summary>The sync service types that have an async twin and were compared.</summary>
        public List<Type> Pairs { get; } = new List<Type>();

        /// <summary>Sync services with no async twin registered.</summary>
        public List<Type> SyncOnly { get; } = new List<Type>();

        /// <summary>Async services with no sync twin registered.</summary>
        public List<Type> AsyncOnly { get; } = new List<Type>();

        /// <summary>Pairs whose handlers or decorators differ.</summary>
        public List<string> Problems { get; } = new List<string>();
    }
}
