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
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.Shared.Basic.Chaos;
using DotNetWorkQueue.Transport.SqlServer.Decorator;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;
using Polly.Contrib.WaitAndRetry;
using System;
using System.Diagnostics;

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
            var tracer = container.GetInstance<ActivitySource>();
            var log = container.GetInstance<ILogger>();

            InjectOutcomePolicy chaosPolicy = null;
            AsyncInjectOutcomePolicy chaosPolicyAsync = null;

            //do not create unless enabled due to the overhead
            if (policies.EnableChaos)
            {
                chaosPolicy = CreateRetryChaos(policies);
                chaosPolicyAsync = CreateRetryChaosAsync(policies);
            }

            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(RetryConstants.FirstWaitInMs), retryCount: RetryConstants.RetryCount);
            var delayAsync = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(RetryConstants.FirstWaitInMs), retryCount: RetryConstants.RetryCount);

            var retrySql = Policy
                 .Handle<SqlException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), ex.Number))
                 .WaitAndRetry(delay,
                     (exception, timeSpan, retryCount, context) =>
                     {
                         log.LogWarning($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occurred {retryCount} times{System.Environment.NewLine}{exception}");
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
                .Handle<SqlException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), ex.Number))
                .WaitAndRetryAsync(delayAsync,
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.LogWarning($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occurred {retryCount} times{System.Environment.NewLine}{exception}");
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

        private static InjectOutcomePolicy CreateRetryChaos(IPolicies policies)
        {
            return MonkeyPolicy.InjectException(with =>
                with.Fault(Behaviour())
                    .InjectionRate((context, token) => ChaosPolicyShared.InjectionRate(context, RetryConstants.RetryCount, RetryAttempts))
                    .Enabled(policies.EnableChaos)
            );
        }

        private static AsyncInjectOutcomePolicy CreateRetryChaosAsync(IPolicies policies)
        {
            return MonkeyPolicy.InjectExceptionAsync(with =>
                with.Fault(Behaviour())
                    .InjectionRate((context, token) => ChaosPolicyShared.InjectionRateAsync(context, RetryConstants.RetryCount, RetryAttempts))
                    .Enabled(policies.EnableChaos)
            );

        }

        private static SqlException Behaviour()
        {
            return MockSqlException.Create();
        }
    }
}
