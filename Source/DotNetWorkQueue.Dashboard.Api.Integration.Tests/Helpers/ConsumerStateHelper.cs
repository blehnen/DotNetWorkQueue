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
using System.Collections.Generic;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers
{
    public class ConsumerStateHelper<TTransportInit> : IDisposable
        where TTransportInit : ITransportInit, new()
    {
        private QueueContainer<TTransportInit> _container;
        private IConsumerQueue _consumer;
        private ManualResetEventSlim _blockSignal;

        public void StartBlockingConsumer(QueueConnection queueConnection,
            ICreationScope scope,
            int workerCount = 1,
            int heartBeatSeconds = 300)
        {
            _blockSignal = new ManualResetEventSlim(false);
            _container = new QueueContainer<TTransportInit>(
                serviceRegister => serviceRegister.RegisterNonScopedSingleton(scope));
            _consumer = _container.CreateConsumer(queueConnection);

            _consumer.Configuration.Worker.WorkerCount = workerCount;
            _consumer.Configuration.Worker.SingleWorkerWhenNoWorkFound = true;
            _consumer.Configuration.HeartBeat.Time = TimeSpan.FromSeconds(heartBeatSeconds);
            _consumer.Configuration.HeartBeat.MonitorTime = TimeSpan.FromSeconds(heartBeatSeconds / 2);

            var signal = _blockSignal;
            _consumer.Start<FakeMessage>((message, notifications) =>
            {
                signal.Wait(TimeSpan.FromSeconds(heartBeatSeconds));
            }, new ConsumerQueueNotifications());
        }

        public void StartErrorConsumer(QueueConnection queueConnection,
            ICreationScope scope,
            CountdownEvent errorCountdown,
            int workerCount = 1)
        {
            _container = new QueueContainer<TTransportInit>(
                serviceRegister => serviceRegister.RegisterNonScopedSingleton(scope));
            _consumer = _container.CreateConsumer(queueConnection);

            _consumer.Configuration.Worker.WorkerCount = workerCount;
            _consumer.Configuration.Worker.SingleWorkerWhenNoWorkFound = true;
            _consumer.Configuration.TransportConfiguration.RetryDelayBehavior.Add(
                typeof(InvalidOperationException),
                new List<TimeSpan>
                {
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(100)
                });

            _consumer.Start<FakeMessage>((message, notifications) =>
            {
                throw new InvalidOperationException("Intentional test error");
            }, new ConsumerQueueNotifications(
                onMessageMovedToErrorQueue: _ => { errorCountdown.Signal(); }));
        }

        public void StartBlockingConsumerShortHeartBeat(QueueConnection queueConnection,
            ICreationScope scope,
            int workerCount = 1)
        {
            _blockSignal = new ManualResetEventSlim(false);
            _container = new QueueContainer<TTransportInit>(
                serviceRegister => serviceRegister.RegisterNonScopedSingleton(scope));
            _consumer = _container.CreateConsumer(queueConnection);

            _consumer.Configuration.Worker.WorkerCount = workerCount;
            _consumer.Configuration.Worker.SingleWorkerWhenNoWorkFound = true;
            // HeartBeat is written at dequeue time (ReceiveMessage sets HeartBeat = NOW in ticks).
            // By NOT setting UpdateTime, the HeartBeatWorker never schedules updates,
            // so HeartBeat stays at its dequeue-time value and becomes stale within seconds.
            // Keep Time large (300s) so the consumer's heartbeat monitor doesn't reset
            // the message back to Waiting before the Dashboard stale query can detect it.
            _consumer.Configuration.HeartBeat.Time = TimeSpan.FromSeconds(300);
            _consumer.Configuration.HeartBeat.MonitorTime = TimeSpan.FromSeconds(150);

            var signal = _blockSignal;
            _consumer.Start<FakeMessage>((message, notifications) =>
            {
                signal.Wait(TimeSpan.FromMinutes(5));
            }, new ConsumerQueueNotifications());
        }

        public void StopBlockingConsumer()
        {
            _blockSignal?.Set();
        }

        public void Dispose()
        {
            _blockSignal?.Set();
            _consumer?.Dispose();
            _container?.Dispose();
            _blockSignal?.Dispose();
        }
    }
}
