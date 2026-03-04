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
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.Shared.Basic.Chaos;
using DotNetWorkQueue.Transport.SqlServer.Decorator;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Polly;
using Polly.Retry;
using Polly.Simmy;
using Polly.Simmy.Fault;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.Shared.Basic;

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

            //RetryCommandHandler
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryCommandHandler,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryCommandHandler,
                    "A policy for retrying a failed command. This checks specific" +
                    "SQL server errors, such as deadlocks, and retries the command" +
                    "after a short pause"));
            policies.Registry.TryAddBuilder(TransportPolicyDefinitions.RetryCommandHandler,
                (builder, _) =>
                {
                    builder.AddRetry(CreateRetryOptions(log, tracer));
                    if (policies.EnableChaos)
                        builder.AddChaosFault(CreateChaosOptions(policies));
                });

            //RetryCommandHandlerAsync
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryCommandHandlerAsync,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryCommandHandler,
                    "A policy for retrying a failed command. This checks specific" +
                    "SQL server errors, such as deadlocks, and retries the command" +
                    "after a short pause"));
            policies.Registry.TryAddBuilder(TransportPolicyDefinitions.RetryCommandHandlerAsync,
                (builder, _) =>
                {
                    builder.AddRetry(CreateRetryOptions(log, tracer));
                    if (policies.EnableChaos)
                        builder.AddChaosFault(CreateChaosOptions(policies));
                });

            //RetryQueryHandler
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryQueryHandler,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryQueryHandler,
                    "A policy for retrying a failed query. This checks specific" +
                    "SQL server errors, such as deadlocks, and retries the query" +
                    "after a short pause"));
            policies.Registry.TryAddBuilder(TransportPolicyDefinitions.RetryQueryHandler,
                (builder, _) =>
                {
                    builder.AddRetry(CreateRetryOptions(log, tracer));
                    if (policies.EnableChaos)
                        builder.AddChaosFault(CreateChaosOptions(policies));
                });
        }

        private static RetryStrategyOptions CreateRetryOptions(ILogger log, ActivitySource tracer)
        {
            return new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => HasRetryableSqlException(ex)),
                MaxRetryAttempts = RetryConstants.RetryCount,
                Delay = TimeSpan.FromMilliseconds(RetryConstants.FirstWaitInMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    log.LogWarning($"An error has occurred; we will try to re-run the transaction in {args.RetryDelay.TotalMilliseconds} ms. An error has occurred {args.AttemptNumber + 1} times{System.Environment.NewLine}{args.Outcome.Exception}");
                    if (Activity.Current != null)
                    {
                        using (var scope = tracer.StartActivity("RetrySqlPolicy"))
                        {
                            try
                            {
                                scope?.SetTag("RetryTime", args.RetryDelay.ToString());
                                if (args.Outcome.Exception != null)
                                    scope?.AddException(args.Outcome.Exception);
                            }
                            finally
                            {
                                scope?.SetEndTime(scope.StartTimeUtc.Add(args.RetryDelay));
                            }
                        }
                    }
                    return default;
                }
            };
        }

        private static ChaosFaultStrategyOptions CreateChaosOptions(IPolicies policies)
        {
            return new ChaosFaultStrategyOptions
            {
                FaultGenerator = _ => new ValueTask<Exception>(
                    MockSqlException.Create((int)ChaosPolicyShared.GetRandomEnum<RetryableSqlErrors>())),
                InjectionRateGenerator = args => new ValueTask<double>(
                    ChaosPolicyShared.InjectionRate(args.Context, RetryConstants.RetryCount, RetryAttempts)),
                EnabledGenerator = args => new ValueTask<bool>(policies.EnableChaos)
            };
        }

        private static bool HasRetryableSqlException(Exception ex)
        {
            while (ex != null)
            {
                if (ex is SqlException sqlEx && Enum.IsDefined(typeof(RetryableSqlErrors), sqlEx.Number))
                    return true;
                ex = ex.InnerException;
            }
            return false;
        }
    }
}
