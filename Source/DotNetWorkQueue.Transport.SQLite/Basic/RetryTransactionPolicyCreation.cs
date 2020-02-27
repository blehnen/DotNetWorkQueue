// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Chaos;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Decorator;
using OpenTracing;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Behavior;

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
            var tracer = container.GetInstance<ITracer>();
            var policies = container.GetInstance<IPolicies>();
            var log = container.GetInstance<ILogFactory>().Create();

            var chaosPolicy = CreateRetryChaos(policies);

            var retrySql = Policy
                .Handle<SQLiteException>(ex => Enum.IsDefined(typeof(RetryableSqlErrors), Convert.ToInt32(ex.ResultCode)))
                .WaitAndRetry(
                    RetryConstants.RetryCount,
                    retryAttempt => TimeSpan.FromMilliseconds(ThreadSafeRandom.Next(RetryConstants.MinWait, RetryConstants.MaxWait)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        log.WarnException($"An error has occurred; we will try to re-run the transaction in {timeSpan.TotalMilliseconds} ms. An error has occured {retryCount} times", exception);
                        if (tracer.ActiveSpan != null)
                        {
                            IScope scope = tracer.BuildSpan("RetryTransaction").StartActive(finishSpanOnDispose: false);
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

        private static InjectBehaviourPolicy CreateRetryChaos(IPolicies policies)
        {
            return MonkeyPolicy.InjectBehaviour((context) => throw new SQLiteException(
                    (SQLiteErrorCode) ChaosPolicyShared.GetRandomEnum<RetryableSqlErrors>(),
                    "Policy chaos testing"),
                (context) => ChaosPolicyShared.InjectionRate(context, RetryConstants.RetryCount, RetryAttempts),
                (context) => policies.EnableChaos);
        }
    }
}
