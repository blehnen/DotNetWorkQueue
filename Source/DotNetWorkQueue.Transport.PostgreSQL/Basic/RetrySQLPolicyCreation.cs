// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.PostgreSQL.Decorator;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Chaos;
using Npgsql;
using OpenTelemetry.Trace;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Behavior;
using Polly.Contrib.Simmy.Outcomes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
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
            var tracer = container.GetInstance<Tracer>();
            var log = container.GetInstance<ILogger>();

            var chaosPolicy = CreateRetryChaos(policies);
            var chaosPolicyAsync = CreateRetryChaosAsync(policies);

            var retrySql = Policy
                .Handle<PostgresException>(ex => ex.IsTransient)
                .WaitAndRetry(
                    RetryConstants.RetryCount,
                    retryAttempt => TimeSpan.FromMilliseconds(ThreadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.LogWarning($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occurred {retryCount} times", exception);
                        if (Tracer.CurrentSpan != null)
                        {
                            var scope = tracer.StartActiveSpan("RetrySqlPolicy");
                            try
                            {
                                scope.SetAttribute("RetryTime", timeSpan.ToString());
                                scope.RecordException(exception);
                            }
                            finally
                            {
                                scope.End(DateTimeOffset.UtcNow.Add(timeSpan));
                            }
                        }
                    });

            var retrySqlAsync = Policy
                .Handle<PostgresException>(ex => ex.IsTransient)
                .WaitAndRetryAsync(
                    RetryConstants.RetryCount,
                    retryAttempt => TimeSpan.FromMilliseconds(ThreadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.LogWarning($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occurred {retryCount} times", exception);
                        if (Tracer.CurrentSpan != null)
                        {
                            var scope = tracer.StartActiveSpan("RetrySqlPolicy");
                            try
                            {
                                scope.SetAttribute("RetryTime", timeSpan.ToString());
                                scope.RecordException(exception);
                            }
                            finally
                            {
                                scope.End(DateTimeOffset.UtcNow.Add(timeSpan));
                            }
                        }
                    });

            //RetryCommandHandler
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryCommandHandler,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryCommandHandler,
                    "A policy for retrying a failed command. This checks specific" +
                    "PostGres server errors, such as deadlocks, and retries the command" +
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
                    "PostGres server errors, such as deadlocks, and retries the command" +
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
                    "PostGres server errors, such as deadlocks, and retries the query" +
                    "after a short pause"));
            if (chaosPolicy != null)
                policies.Registry[TransportPolicyDefinitions.RetryQueryHandler] = retrySql.Wrap(chaosPolicy);
            else
                policies.Registry[TransportPolicyDefinitions.RetryQueryHandler] = retrySql;
        }

        private static InjectOutcomePolicy CreateRetryChaos(IPolicies policies)
        {
            var fault = new PostgresException(string.Empty, string.Empty, string.Empty,
                ChaosPolicyShared.GetRandomString(RetryablePostGreErrors.Errors.ToList()));

            return MonkeyPolicy.InjectException(with =>
                with.Fault(fault)
                    .InjectionRate((context, token) => ChaosPolicyShared.InjectionRate(context, RetryConstants.RetryCount, RetryAttempts))
                    .Enabled(policies.EnableChaos)
            );
        }

        private static AsyncInjectOutcomePolicy  CreateRetryChaosAsync(IPolicies policies)
        {
            var fault = new PostgresException(string.Empty, string.Empty, string.Empty,
                ChaosPolicyShared.GetRandomString(RetryablePostGreErrors.Errors.ToList()));

            return MonkeyPolicy.InjectExceptionAsync(with =>
                with.Fault(fault)
                    .InjectionRate((context, token) => ChaosPolicyShared.InjectionRateAsync(context, RetryConstants.RetryCount, RetryAttempts))
                    .Enabled(policies.EnableChaos)
            );
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
            throw new PostgresException(string.Empty, string.Empty, string.Empty,
                ChaosPolicyShared.GetRandomString(RetryablePostGreErrors.Errors.ToList()));
        }
    }
}
