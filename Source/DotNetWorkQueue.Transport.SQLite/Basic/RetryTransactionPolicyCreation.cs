// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Data.SQLite;
using System.Diagnostics;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Chaos;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Decorator;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Behavior;
using Polly.Contrib.Simmy.Outcomes;
using Polly.Contrib.WaitAndRetry;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Creates a policy that will re-try specific transactions based on SQLite error code
    /// </summary>
    public static class RetryTransactionPolicyCreation
    {
        private const string RetryAttempts = "RetryAttempts";

        /// <summary>
        /// Registers the retry policy in the container
        /// </summary>
        /// <param name="container">The container.</param>
        public static void Register(IContainer container)
        {
            var tracer = container.GetInstance<ActivitySource>();
            var policies = container.GetInstance<IPolicies>();
            var log = container.GetInstance<ILogger>();

            var chaosPolicy = CreateRetryChaos(policies);
            var chaosPolicyAsync = CreateRetryChaosAsync(policies);

            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(RetryConstants.FirstWaitInMs), retryCount: RetryConstants.RetryCount);
            var delayAsync = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(RetryConstants.FirstWaitInMs), retryCount: RetryConstants.RetryCount);

            var retrySql = Policy
                .Handle<SQLiteException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), (RetryableSqlErrors)ex.ErrorCode))
                //.Handle<SQLiteException>()
                .WaitAndRetry(delay,
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.LogWarning($"An error has occurred; we will try to re-run the statement in {timeSpan.TotalMilliseconds} ms. An error has occurred {retryCount} times{System.Environment.NewLine}{exception}");
                        if (Activity.Current != null)
                        {
                            using (var scope = tracer.StartActivity("RetrySqlPolicy"))
                            {
                                try
                                {
                                    scope?.SetTag("RetryTime", timeSpan.ToString());
                                    scope?.AddException(exception);
                                }
                                finally
                                {
                                    scope?.SetEndTime(scope.StartTimeUtc.Add(timeSpan));
                                }
                            }
                        }
                    });

            var retrySqlAsync = Policy
                .Handle<SQLiteException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), (RetryableSqlErrors)ex.ErrorCode))
                //.Handle<SQLiteException>()
                .WaitAndRetryAsync(delayAsync,
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.LogWarning($"An error has occurred; we will try to re-run the statement in {timeSpan.TotalMilliseconds} ms. An error has occurred {retryCount} times{System.Environment.NewLine}{exception}");
                        if (Activity.Current != null)
                        {
                            using (var scope = tracer.StartActivity("RetrySqlPolicy"))
                            {
                                try
                                {
                                    scope?.SetTag("RetryTime", timeSpan.ToString());
                                    scope?.AddException(exception);
                                }
                                finally
                                {
                                    scope?.SetEndTime(scope.StartTimeUtc.Add(timeSpan));
                                }
                            }
                        }
                    });


            //RetryCommandHandler
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryCommandHandler,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryCommandHandler,
                    "A policy for retrying a failed command. This checks specific" +
                    "Sqlite errors, such as deadlocks, and retries the command" +
                    "after a short pause"));
            if (chaosPolicy != null)
                policies.Registry[TransportPolicyDefinitions.RetryCommandHandler] = retrySql.Wrap(chaosPolicy);
            else
                policies.Registry[TransportPolicyDefinitions.RetryCommandHandler] = retrySql;

            //RetryCommandHandlerAsync
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryCommandHandlerAsync,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryCommandHandlerAsync,
                    "A policy for retrying a failed command. This checks specific" +
                    "Sqlite errors, such as deadlocks, and retries the command" +
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
                    "Sqlite errors, such as deadlocks, and retries the query" +
                    "after a short pause"));
            if (chaosPolicy != null)
                policies.Registry[TransportPolicyDefinitions.RetryQueryHandler] = retrySql.Wrap(chaosPolicy);
            else
                policies.Registry[TransportPolicyDefinitions.RetryQueryHandler] = retrySql;

            //BeginTransaction
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.BeginTransaction,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.BeginTransaction,
                    "A policy for retrying a BeginTransaction command. Sqlite will fail to start" +
                    "a transaction if another one is in progress. This behavior lets us wait a little" +
                    "bit and try again"));
            if (chaosPolicy != null)
                policies.Registry[TransportPolicyDefinitions.BeginTransaction] = retrySql.Wrap(chaosPolicy);
            else policies.Registry[TransportPolicyDefinitions.BeginTransaction] = retrySql;
        }

        private static InjectOutcomePolicy CreateRetryChaos(IPolicies policies)
        {
            var fault = new SQLiteException(
                (SQLiteErrorCode)ChaosPolicyShared.GetRandomEnum<RetryableSqlErrors>(),
                "Policy chaos testing");

            return MonkeyPolicy.InjectException(with =>
                with.Fault(fault)
                    .InjectionRate((context, token) => ChaosPolicyShared.InjectionRate(context, RetryConstants.RetryCount, RetryAttempts))
                    .Enabled(policies.EnableChaos)
            );
        }
        private static AsyncInjectOutcomePolicy CreateRetryChaosAsync(IPolicies policies)
        {
            var fault = new SQLiteException(
                (SQLiteErrorCode)ChaosPolicyShared.GetRandomEnum<RetryableSqlErrors>(),
                "Policy chaos testing");

            return MonkeyPolicy.InjectExceptionAsync(with =>
                with.Fault(fault)
                    .InjectionRate((context, token) => ChaosPolicyShared.InjectionRateAsync(context, RetryConstants.RetryCount, RetryAttempts))
                    .Enabled(policies.EnableChaos)
            );

        }
    }
}
