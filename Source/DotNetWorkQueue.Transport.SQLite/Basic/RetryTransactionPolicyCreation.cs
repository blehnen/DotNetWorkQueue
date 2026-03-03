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
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Polly.Simmy;
using Polly.Simmy.Fault;

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

            //RetryCommandHandler
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.RetryCommandHandler,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.RetryCommandHandler,
                    "A policy for retrying a failed command. This checks specific" +
                    "Sqlite errors, such as deadlocks, and retries the command" +
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
                    TransportPolicyDefinitions.RetryCommandHandlerAsync,
                    "A policy for retrying a failed command. This checks specific" +
                    "Sqlite errors, such as deadlocks, and retries the command" +
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
                    "Sqlite errors, such as deadlocks, and retries the query" +
                    "after a short pause"));
            policies.Registry.TryAddBuilder(TransportPolicyDefinitions.RetryQueryHandler,
                (builder, _) =>
                {
                    builder.AddRetry(CreateRetryOptions(log, tracer));
                    if (policies.EnableChaos)
                        builder.AddChaosFault(CreateChaosOptions(policies));
                });

            //BeginTransaction
            policies.TransportDefinition.TryAdd(TransportPolicyDefinitions.BeginTransaction,
                new TransportPolicyDefinition(
                    TransportPolicyDefinitions.BeginTransaction,
                    "A policy for retrying a BeginTransaction command. Sqlite will fail to start" +
                    "a transaction if another one is in progress. This behavior lets us wait a little" +
                    "bit and try again"));
            policies.Registry.TryAddBuilder(TransportPolicyDefinitions.BeginTransaction,
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
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => HasRetryableSQLiteException(ex)),
                MaxRetryAttempts = RetryConstants.RetryCount,
                Delay = TimeSpan.FromMilliseconds(RetryConstants.FirstWaitInMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    log.LogWarning($"An error has occurred; we will try to re-run the statement in {args.RetryDelay.TotalMilliseconds} ms. An error has occurred {args.AttemptNumber + 1} times{System.Environment.NewLine}{args.Outcome.Exception}");
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
                    new SQLiteException(
                        (SQLiteErrorCode)ChaosPolicyShared.GetRandomEnum<RetryableSqlErrors>(),
                        "Policy chaos testing")),
                InjectionRateGenerator = args => new ValueTask<double>(
                    ChaosPolicyShared.InjectionRate(args.Context, RetryConstants.RetryCount, RetryAttempts)),
                EnabledGenerator = args => new ValueTask<bool>(policies.EnableChaos)
            };
        }

        private static bool HasRetryableSQLiteException(Exception ex)
        {
            while (ex != null)
            {
                if (ex is SQLiteException sqliteEx && Enum.IsDefined(typeof(RetryableSqlErrors), (RetryableSqlErrors)sqliteEx.ErrorCode))
                    return true;
                ex = ex.InnerException;
            }
            return false;
        }
    }
}
