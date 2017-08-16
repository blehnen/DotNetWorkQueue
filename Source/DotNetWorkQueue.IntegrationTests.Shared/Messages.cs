// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Tynamix.ObjectFiller;
using System.Threading;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    public class FakeResponse
    {
        public string ResponseMessage { get; set; }
    }
    public class FakeMessage
    {
        public FakeMessage()
        {
            MoreInfo = new List<FakeSubClass>();
        }
        public string Name { get; set; }
        public DateTime BornOn { get; set; }
        public string HomePage { get; set; }
        public decimal Amount { get; set; }
        public bool Allowed { get; set; }
        public List<FakeSubClass> MoreInfo { get; set; }

        public string Id
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append(Name);
                builder.Append(BornOn);
                builder.Append(HomePage);
                builder.Append(Amount);
                builder.Append(Allowed);
                foreach (var data in MoreInfo)
                {
                    builder.Append(data.MoreInfo);
                }
                return builder.ToString();
            }
        }
    }

    public class FakeSubClass
    {
        public string MoreInfo { get; set; }
    }

    public class FakeMessageA
    {
        public string Name { get; set; }
        public DateTime BornOn { get; set; }
        public string HomePage { get; set; }
        public decimal Amount { get; set; }
        public bool Allowed { get; set; }
    }

    public class FakeMessageB
    {
        public string Name { get; set; }
        public bool Allowed { get; set; }
    }
    public static class GenerateMessage
    {
        public static TMessage Create<TMessage>()
            where TMessage: class
        {
            var pFiller = new Filler<TMessage>();
            return pFiller.Create();
        }
    }

    public static class GenerateMethod
    {
        public static Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> CreateCompiled(Guid id, int runTime)
        {
            return (message, workerNotification) => StandardTesting.Run(id, runTime);
        }

        public static Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> CreateRollBackCompiled(Guid id, int runTime)
        {
            return (message, workerNotification) => RollBackTesting.Run(message, id, runTime);
        }

        public static Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> CreateNoOpCompiled(Guid id, int runTime)
        {
            return (message, workerNotification) => StandardTesting.NoOp();
        }

        public static Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> CreateCancelCompiled(Guid id, int runTime)
        {
            return (message, workerNotification) => CancelTesting.Run(message, workerNotification, id, runTime);
        }

        public static Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> CreateErrorCompiled(Guid id, int runTime)
        {
            return (message, workerNotification) => StandardTesting.Error();
        }

        public static Expression<Func<IReceivedMessage<MessageExpression>, IWorkerNotification, object>> CreateRpcCompiled(
            Guid id, int runTime)
        {
            return (message, workerNotification) => StandardTesting.RunRpc(id, runTime);
        }

        public static LinqExpressionToRun CreateMultipleDynamic(Guid id, int counter, int runTime)
        {
            return CreateDefaultLinq($"(message, workerNotification) => StandardTesting.Run(new Guid(\"{id}\"), int.Parse(\"{runTime}\"), int.Parse(\"{counter}\"))", true);
        }
        public static LinqExpressionToRun CreateDynamic(Guid id, int runTime)
        {
            return CreateDefaultLinq($"(message, workerNotification) => StandardTesting.Run(new Guid(\"{id}\"), int.Parse(\"{runTime}\"))");
        }

        public static LinqExpressionToRun CreateRollBackDynamic(Guid id, int runTime)
        {
            return CreateDefaultLinq($"(message, workerNotification) => RollBackTesting.Run((IReceivedMessage<MessageExpression>)message, new Guid(\"{id}\"), int.Parse(\"{runTime}\"))");
        }

        public static LinqExpressionToRun CreateNoOpDynamic(Guid id, int runTime)
        {
            return CreateDefaultLinq("(message, workerNotification) => StandardTesting.NoOp()");
        }

        public static LinqExpressionToRun CreateCancelDynamic(Guid id, int runTime)
        {
            return
                CreateDefaultLinq($"(message, workerNotification) => CancelTesting.Run((IReceivedMessage<MessageExpression>)message, (IWorkerNotification)workerNotification, new Guid(\"{id}\"), int.Parse(\"{runTime}\"))");
        }

        public static LinqExpressionToRun CreateErrorDynamic(Guid id, int runTime)
        {
            return CreateDefaultLinq("(message, workerNotification) => StandardTesting.Error()");
        }

        public static LinqExpressionToRun CreateRpcDynamic(Guid id, int runTime)
        {
            return CreateDefaultLinq($"(message, workerNotification) => StandardTesting.RunRpc(new Guid(\"{id}\"), int.Parse(\"{runTime}\"))");
        }

        private static LinqExpressionToRun CreateDefaultLinq(string method, bool unique = false)
        {
            return new LinqExpressionToRun(method, new List<string> { "DotNetWorkQueue.IntegrationTests.Shared.dll" }, new List<string> { "DotNetWorkQueue.IntegrationTests.Shared" }, unique);
        }

        public static void ClearCancel(Guid id)
        {
            CancelTesting.Clear(id);
        }
        public static void ClearStandard(Guid id)
        {
            StandardTesting.Clear(id);
        }
        public static void ClearRollback(Guid id)
        {
            RollBackTesting.Clear(id);
        }
    }

    public static class StandardTesting
    {
        public static void Clear(Guid queueId)
        {
            MethodIncrementWrapper.Clear(queueId);
        }
        public static void Run(Guid queueId, int runTime)
        {
            if (runTime > 0)
                Thread.Sleep(runTime * 1000);

            MethodIncrementWrapper.IncreaseCounter(queueId);
        }
        public static void Run(Guid queueId, int runTime, int counter)
        {
            if (runTime > 0)
                Thread.Sleep(runTime * 1000);

            MethodIncrementWrapper.IncreaseCounter(queueId);
        }
        public static object RunRpc(Guid queueId, int runTime)
        {
            if (runTime > 0)
                Thread.Sleep(runTime * 1000);

            MethodIncrementWrapper.IncreaseCounter(queueId);

            var rc = new FakeResponse {ResponseMessage = "int test"};
            return rc;
        }
        public static void NoOp()
        {

        }

        public static void Error()
        {
            throw new IndexOutOfRangeException("The index is out of range");
        }
    }

    public static class CancelTesting
    {
        public static void Clear(Guid queueId)
        {
            MethodIncrementWrapper.Clear(queueId);
        }
        public static void Run<TMessage>(IReceivedMessage<TMessage> message, IWorkerNotification notification, Guid queueId, int runTime)
            where TMessage : class
        {
            if (MethodIncrementWrapper.HasRollBack(queueId, (Guid)message.CorrelationId.Id.Value))
            {
                var counter = runTime / 3;
                for (int i = 0; i < counter; i++)
                {
                    if (notification.WorkerStopping.StopWorkToken.IsCancellationRequested || notification.WorkerStopping.CancelWorkToken.IsCancellationRequested)
                    {
                        MethodIncrementWrapper.IncreaseCounter(queueId);
                        return;
                    }
                    Thread.Sleep(1000);
                }
                MethodIncrementWrapper.IncreaseCounter(queueId);
            }
            else
            {
                var counter = runTime/2;
                for (int i = 0; i < counter; i++)
                {
                    Thread.Sleep(1000);
                }
                MethodIncrementWrapper.SetRollback(queueId, (Guid)message.CorrelationId.Id.Value);
                throw new OperationCanceledException("I don't feel like processing this message");
            }
        }
    }
    public static class RollBackTesting
    {
        public static void Clear(Guid queueId)
        {
            MethodIncrementWrapper.Clear(queueId);
        }
        public static void Run<TMessage>(IReceivedMessage<TMessage> message, Guid queueId, int runTime)
            where TMessage: class
        {
            if (MethodIncrementWrapper.HasRollBack(queueId, (Guid)message.CorrelationId.Id.Value))
            {
                if (runTime > 0)
                    Thread.Sleep(runTime * 1000);

                MethodIncrementWrapper.IncreaseCounter(queueId);
            }
            else
            {
                MethodIncrementWrapper.SetRollback(queueId, (Guid)message.CorrelationId.Id.Value);
                throw new OperationCanceledException("I don't feel like processing this message");
            }
        }
    }
}
