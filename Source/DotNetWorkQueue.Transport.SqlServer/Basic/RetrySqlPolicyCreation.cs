using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Chaos;
using DotNetWorkQueue.Transport.SqlServer.Decorator;
using OpenTracing;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Behavior;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Creates a policy that will re-try specific SQL statements based on the exception
    /// </summary>
    public static class RetrySqlPolicyCreation
    {
        private const string RetryAttempts = "RetryAttempts";

        /// <summary>
        /// Registers the policies in the container
        /// </summary>
        /// <param name="container">The container.</param>
        public static void Register(IContainer container)
        {
            var policies = container.GetInstance<IPolicies>();
            var tracer = container.GetInstance<ITracer>();
            var log = container.GetInstance<ILogFactory>().Create();

            var chaosPolicy = CreateRetryChaos(policies);
            var chaosPolicyAsync = CreateRetryChaosAsync(policies);

            var retrySql = Policy
                 .Handle<SqlException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), ex.Number))
                 .WaitAndRetry(
                     RetryConstants.RetryCount,
                     retryAttempt => TimeSpan.FromMilliseconds(ThreadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait)),
                     (exception, timeSpan, retryCount, context) =>
                     {
                         log.WarnException($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occured {retryCount} times", exception);
                         if (tracer.ActiveSpan != null)
                         {
                             IScope scope = tracer.BuildSpan("RetrySqlPolicy").StartActive(finishSpanOnDispose: false);
                             try
                             {
                                 scope.Span.SetTag("RetryTime", timeSpan.ToString());
                                 scope.Span.Log(exception.ToString());
                             }
                             finally
                             {
                                 scope.Span.Finish(DateTimeOffset.UtcNow.Add(timeSpan));
                             }
                         }
                     });

            var retrySqlAsync = Policy
                .Handle<SqlException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), ex.Number))
                .WaitAndRetryAsync(
                    RetryConstants.RetryCount,
                    retryAttempt => TimeSpan.FromMilliseconds(ThreadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.WarnException($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occured {retryCount} times", exception);
                        if (tracer.ActiveSpan != null)
                        {
                            IScope scope = tracer.BuildSpan("RetrySqlPolicy").StartActive(finishSpanOnDispose: false);
                            try
                            {
                                scope.Span.SetTag("RetryTime", timeSpan.ToString());
                                scope.Span.Log(exception.ToString());
                            }
                            finally
                            {
                                scope.Span.Finish(DateTimeOffset.UtcNow.Add(timeSpan));
                            }
                        }
                    });

            //RetryCommandHandler
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryCommandHandler,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryCommandHandler,
                    "A policy for retrying a failed command. This checks specific" +
                    "SQL server errors, such as deadlocks, and retries the command" +
                    "after a short pause"));
            if (chaosPolicy != null)
                policies.Registry[TransportPolicyDefinitions.RetryCommandHandler] = retrySql.Wrap(chaosPolicy);
            else
                policies.Registry[TransportPolicyDefinitions.RetryCommandHandler] = retrySql;

            //RetryCommandHandlerAsync
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryCommandHandlerAsync,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryCommandHandler,
                    "A policy for retrying a failed command. This checks specific" +
                    "SQL server errors, such as deadlocks, and retries the command" +
                    "after a short pause"));
            if (chaosPolicyAsync != null)
                policies.Registry[TransportPolicyDefinitions.RetryCommandHandlerAsync] = retrySqlAsync.WrapAsync(chaosPolicyAsync);
            else
                policies.Registry[TransportPolicyDefinitions.RetryCommandHandlerAsync] = retrySqlAsync;

            //RetryQueryHandler
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryQueryHandler,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryQueryHandler,
                    "A policy for retrying a failed query. This checks specific" +
                    "SQL server errors, such as deadlocks, and retries the query" +
                    "after a short pause"));
            if (chaosPolicy != null)
                policies.Registry[TransportPolicyDefinitions.RetryQueryHandler] = retrySql.Wrap(chaosPolicy);
            else
                policies.Registry[TransportPolicyDefinitions.RetryQueryHandler] = retrySql;
        }

        private static InjectBehaviourPolicy CreateRetryChaos(IPolicies policies)
        {
            return MonkeyPolicy.InjectBehaviour(Behaviour,
                (context) => ChaosPolicyShared.InjectionRate(context, RetryConstants.RetryCount, RetryAttempts),
                (context) => policies.EnableChaos);
        }

        private static AsyncInjectBehaviourPolicy CreateRetryChaosAsync(IPolicies policies)
        {
            return MonkeyPolicy.InjectBehaviourAsync(Behaviour,
                (context) => InjectionRate(context, RetryConstants.RetryCount, RetryAttempts),
                (context) => Enabled(context, policies));

        }

        private static async Task<bool> Enabled(Context arg, IPolicies policy)
        {
            return await ChaosPolicyShared.RunAsync(() => policy.EnableChaos);
        }

        private static async Task<double> InjectionRate(Context arg, int retryAttempts, string keyName)
        {
            return await ChaosPolicyShared.InjectionRateAsync(arg, retryAttempts, keyName);
        }

        private static Task Behaviour(Context arg1, CancellationToken arg2)
        {
            SqlError sqlError = null;
#if NETFULL
            sqlError = CreateInstance<SqlError>(Convert.ToInt32(ChaosPolicyShared.GetRandomEnum<RetryableSqlErrors>()), null, null, null, null, null, null);
#else
            sqlError = CreateInstance<SqlError>(Convert.ToInt32(ChaosPolicyShared.GetRandomEnum<RetryableSqlErrors>()), null, null, null, null, null, null, null);
#endif
            var collection = CreateInstance<SqlErrorCollection>();
            ArrayList errors = collection.GetPrivateFieldValue<ArrayList>("errors");
            errors.Add(sqlError);
            var e = CreateInstance<SqlException>(string.Empty, collection, null, Guid.NewGuid());
            throw e;
        }

        private static void Behaviour(Context arg1)
        {
            SqlError sqlError = null;
#if NETFULL
            sqlError = CreateInstance<SqlError>(Convert.ToInt32(ChaosPolicyShared.GetRandomEnum<RetryableSqlErrors>()), null, null, null, null, null, null);
#else
            sqlError = CreateInstance<SqlError>(Convert.ToInt32(ChaosPolicyShared.GetRandomEnum<RetryableSqlErrors>()), null, null, null, null, null, null, null);
#endif
            var collection = CreateInstance<SqlErrorCollection>();
#if NETFULL
             var errors = collection.GetPrivateFieldValue<ArrayList>("errors");
             errors.Add(sqlError);
#else
            var errors = collection.GetPrivateFieldValue<List<object>> ("_errors");
            errors.Add(sqlError);
#endif
            var e = CreateInstance<SqlException>(string.Empty, collection, null, Guid.NewGuid());
            throw e;
        }

        private static T CreateInstance<T>(params object[] args)
        {
            var type = typeof(T);
            var assembly = type.Assembly;
            var instance = assembly.CreateInstance(
                type.FullName, true,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, args, null, null);
            return (T)instance;
        }
        /// <summary>
        /// Returns a _private_ Property Value from a given Object. Uses Reflection.
        /// Throws a ArgumentOutOfRangeException if the Property is not found.
        /// </summary>
        /// <typeparam name="T">Type of the Property</typeparam>
        /// <param name="obj">Object from where the Property Value is returned</param>
        /// <param name="propName">Propertyname as string.</param>
        /// <returns>PropertyValue</returns>
        private static T GetPrivateFieldValue<T>(this object obj, string propName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            var pi = obj.GetType().GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi == null) throw new ArgumentOutOfRangeException("propName", string.Format("Property {0} was not found in Type {1}", propName, obj.GetType().FullName));
            return (T)pi.GetValue(obj);
        }
    }
}
