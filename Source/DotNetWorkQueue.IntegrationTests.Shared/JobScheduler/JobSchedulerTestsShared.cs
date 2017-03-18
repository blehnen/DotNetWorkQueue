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
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.Messages;
using FluentAssertions;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.JobScheduler
{
    public class JobSchedulerTestsShared
    {
        private const string Job1 = "job1";
        private const string Job2 = "job2";

        private bool _queueStarted;
        private readonly object _queueStartLocker = new object();
        private IGetTimeFactory _timeFactory;

        public void RunEnqueueTestCompiled<TTransportInit, TJobQueueCreator>(string queueName,
            string connectionString,
            bool addInterceptors,
            Action<string, string, long> verify,
            Action<string, string> setErrorFlag,
            IGetTimeFactory timeFactory)
            where TTransportInit : ITransportInit, new()
            where TJobQueueCreator : class, IJobQueueCreation
        {
            _timeFactory = timeFactory;
            RunEnqueueTest<TTransportInit>(queueName, connectionString, addInterceptors, verify,
                setErrorFlag,
                (x, name) => x.AddUpdateJob<TTransportInit, TJobQueueCreator>(name, queueName, connectionString,
                    "min(*)",
                    (message, workerNotification) => Console.WriteLine(message.MessageId.Id.Value), null, config => { }),

                (x, name, time) => x.AddUpdateJob<TTransportInit, TJobQueueCreator>(name, queueName, connectionString,
                    "min(*)",
                    (message, workerNotification) => Console.WriteLine(message.MessageId.Id.Value), null,  config => { }, true, time), timeFactory

                );
        }

        public void RunEnqueueTestDynamic<TTransportInit, TJobQueueCreator>(string queueName,
            string connectionString,
            bool addInterceptors,
            Action<string, string, long> verify,
            Action<string, string> setErrorFlag,
            IGetTimeFactory timeFactory)
            where TTransportInit : ITransportInit, new()
            where TJobQueueCreator : class, IJobQueueCreation
        {
            _timeFactory = timeFactory;
            using (var jobQueueCreation =
                new JobQueueCreationContainer<TTransportInit>())
            {
                using (
                    var createQueue = jobQueueCreation.GetQueueCreation<TJobQueueCreator>(queueName,
                        connectionString)
                    )
                {
                    RunEnqueueTest<TTransportInit>(queueName, connectionString, addInterceptors, verify,
                        setErrorFlag,
                        (x, name) => x.AddUpdateJob<TTransportInit>(createQueue, name, queueName, connectionString,
                            "min(*)",
                            new LinqExpressionToRun(
                                "(message, workerNotification) => Console.WriteLine(DateTime.Now.Ticks)")),

                        (x, name, time) =>
                            x.AddUpdateJob<TTransportInit>(createQueue, name, queueName, connectionString,
                                "min(*)",
                                new LinqExpressionToRun(
                                    "(message, workerNotification) => Console.WriteLine(DateTime.Now.Ticks)"), null, null, true,
                                time), timeFactory
                        );
                }
            }
        }

        public
            void RunTestMultipleProducers<TTransportInit, TJobQueueCreator>(string queueName,
                string connectionString,
                bool addInterceptors,
                long producerCount,
                IGetTimeFactory timeFactory)
            where TTransportInit : ITransportInit, new()
            where TJobQueueCreator : class, IJobQueueCreation
        {
            var enqueued = 0;
            Exception lastError = null;
            _queueStarted = false;

            using (var jobQueueCreation =
                                new JobQueueCreationContainer<TTransportInit>())
            {
                using (
                    var createQueue = jobQueueCreation.GetQueueCreation<TJobQueueCreator>(queueName,
                        connectionString)
                    )
                {

                    createQueue.CreateJobSchedulerQueue(null, queueName, connectionString);

                    //always run a consumer to clear out jobs
                    using (var queueContainer = new QueueContainer<TTransportInit>(QueueContainer))
                    {
                        using (var queue = queueContainer.CreateMethodConsumer(queueName, connectionString))
                        {
                            queue.Configuration.Worker.WorkerCount = 4;
                            WaitForRollover(timeFactory);

                            Thread.Sleep(7000);

                            Parallel.For(0, producerCount, (i, loopState) =>
                            {
                                    using (var jobContainer = new JobSchedulerContainer())
                                    {
                                        using (var scheduler = CreateScheduler(jobContainer, addInterceptors))
                                        {
                                            scheduler.OnJobQueue +=
                                                (job, message) => Interlocked.Increment(ref enqueued);
                                            scheduler.OnJobQueueException +=
                                                (job, exception) => lastError = exception;
                                            scheduler.Start();

                                            scheduler.AddUpdateJob<TTransportInit, TJobQueueCreator>(Job1, queueName,
                                                connectionString,
                                                "min(*)",
                                                (message, workerNotification) => Console.Write(""));

                                            scheduler.AddUpdateJob<TTransportInit, TJobQueueCreator>(Job2, queueName,
                                                connectionString,
                                                "min(*)",
                                                (message, workerNotification) =>
                                                    Console.Write(""));

                                            WaitForRollover(timeFactory);
                                            StartConsumer(queue);
                                            WaitForEnQueue();
                                        }
                                    }
                                });

                            ValidateEnqueueMultipleProducer(queueName, connectionString, enqueued,
                                lastError, 2);
                        }
                    }
                }
            }
        }

        private void StartConsumer(IConsumerMethodQueue queue)
        {
            if (_queueStarted)
                return;

            lock (_queueStartLocker)
            {
                if (!_queueStarted)
                {
                    queue.Start();
                    _queueStarted = true;
                }
            }
        }

        private void RunEnqueueTest<TTransportInit>(string queueName,
            string connectionString,
            bool addInterceptors,
            Action<string, string, long> verify,
            Action<string, string> setErrorFlag,
            Func<IJobScheduler, string, IScheduledJob> enqueue,
            Func<IJobScheduler, string, TimeSpan, IScheduledJob> enqueueWindow,
            IGetTimeFactory timeFactory)
            where TTransportInit : ITransportInit, new()
        {
            using (var jobContainer = new JobSchedulerContainer(RegisterService))
            {
                using (var scheduler = CreateScheduler(jobContainer, addInterceptors))
                {
                    var enqueued = 0;
                    var nonFatal = 0;
                    Exception lastError = null;
                    scheduler.OnJobQueue += (job, message) => enqueued++;
                    scheduler.OnJobQueueException += (job, exception) => lastError = exception;
                    scheduler.OnJobNonFatalFailureQueue += (job, message) => nonFatal++;
                    scheduler.Start();

                    WaitForRollover(timeFactory);

                    var job1 = enqueue(scheduler, Job1);
                    var job2 = enqueue(scheduler, Job2); //job2 won't be referenced again, but ensures that we have multiple records in the queue for the first test

                    WaitForEnQueue();

                    ValidateEnqueue(queueName, connectionString, verify, enqueued, lastError, nonFatal, 2);

                    enqueued = 0;

                    //remove job2 from schedule - doesn't remove already queued work
                    job2.StopSchedule();
                    scheduler.RemoveJob(Job2);

                    WaitForRollover(timeFactory);

                    WaitForEnQueue();

                    //validate job1 is not queued a second time. There will still be 2 jobs in the transport storage (job1, job2)
                    ValidateNonFatalError(queueName, connectionString, verify, enqueued, lastError, nonFatal, 2);

                    RunConsumer<TTransportInit>(queueName, connectionString);
                    verify(queueName, connectionString, 0);

                    enqueued = 0;
                    nonFatal = 0;
                    WaitForRollover(timeFactory);

                    WaitForEnQueue();

                    ValidateEnqueue(queueName, connectionString, verify, enqueued, lastError, nonFatal, 1);

                    //validate that errors are replaced
                    setErrorFlag(queueName, connectionString);
                    enqueued = 0;
                    nonFatal = 0;
                    WaitForRollover(timeFactory);
                    WaitForEnQueue();
                    ValidateEnqueue(queueName, connectionString, verify, enqueued, lastError, nonFatal, 1);

                    RunConsumer<TTransportInit>(queueName, connectionString);
                    verify(queueName, connectionString, 0);

                    enqueued = 0;
                    nonFatal = 0;
                    job1.StopSchedule();
                    scheduler.RemoveJob(Job1);
                    WaitForRollover(timeFactory);
                    WaitForEnQueue(); //nothing will be queued, make sure we are past fire time
                    enqueueWindow(scheduler, Job1, TimeSpan.FromSeconds(40)); //should be fired right away, since we are inside the window
                    Thread.Sleep(5000);
                    ValidateEnqueue(queueName, connectionString, verify, enqueued, lastError, nonFatal, 1);
                }
            }
        }

        private void RegisterService(IContainer container)
        {
            container.Register(() => _timeFactory, LifeStyles.Singleton);
        }

        private void WaitForEnQueue()
        {
            Thread.Sleep(10000);
        }
        private void WaitForRollover(IGetTimeFactory timeFactory)
        {
            var getTime = timeFactory.Create();
            while (getTime.GetCurrentUtcDate().Second != 55)
            {
                Thread.Sleep(100);
            }
        }
        private void ValidateEnqueue(string queueName, string connectionString, Action<string, string, long> verify,
            long enqueued, Exception error, long nonFatal, long expectedEnqueue)
        {
            if (error != null)
            {
                throw new Exception("Fatal error!", error);
            }
            Assert.Equal(expectedEnqueue, enqueued);
            Assert.Equal(0, nonFatal);
            verify(queueName, connectionString, expectedEnqueue);
        }
        private void ValidateEnqueueMultipleProducer(string queueName, string connectionString,
            long enqueued, Exception error, long expectedEnqueue)
        {
            if (error != null)
            {
                throw new Exception("Fatal error!", error);
            }
            Assert.Equal(expectedEnqueue, enqueued);
        }
        private void ValidateNonFatalError(string queueName, string connectionString, Action<string, string, long> verify,
            long enqueued, Exception error, long nonFatal, long inQueueCount)
        {
            Assert.Equal(0, enqueued);
            error.Should().BeNull("no errors should occur");
            Assert.Equal(1, nonFatal);
            verify(queueName, connectionString, inQueueCount);
        }
        private void RunConsumer<TTransportInit>(string queueName,
            string connectionString)
            where TTransportInit : ITransportInit, new()
        {
            {
                using (var queueContainer = new QueueContainer<TTransportInit>(QueueContainer))
                {
                    using (var queue = queueContainer.CreateMethodConsumer(queueName, connectionString))
                    {
                        queue.Configuration.Worker.WorkerCount = 1;
                        queue.Start();
                        Thread.Sleep(7500);
                    }
                }
            }
        }

        private IJobScheduler CreateScheduler(JobSchedulerContainer container, bool addInterceptors)
        {
            if (!addInterceptors)
            {
                return container.CreateJobScheduler();
            }
            return container.CreateJobScheduler(x => { }, QueueContainer);
        }

        private void QueueContainer(IContainer container)
        {
            container.RegisterCollection<IMessageInterceptor>(new[]
            {
                typeof(GZipMessageInterceptor), //gzip compression
                typeof(TripleDesMessageInterceptor) //encryption
            });
            container.Register(
                    () =>
                        new TripleDesMessageInterceptorConfiguration(
                            Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                            Convert.FromBase64String("aaaaaaaaaaa=")), LifeStyles.Singleton);
        }
    }
}
