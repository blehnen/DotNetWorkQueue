using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.Logging;
#if NETFULL
using DotNetWorkQueue.Messages;
#endif
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

// ReSharper disable AccessToDisposedClosure

namespace DotNetWorkQueue.IntegrationTests.Shared.JobScheduler
{
    public class JobSchedulerTestsShared
    {
        private const string Job1 = "job1";
        private const string Job2 = "job2";

        private bool _queueStarted;
        private readonly object _queueStartLocker = new object();
        private IGetTimeFactory _timeFactory;
        private ILogger _logProvider;
        private ICreationScope _scope;

        public void RunEnqueueTestCompiled<TTransportInit, TJobQueueCreator>(QueueConnection queueConnection,
            bool addInterceptors,
            Action<QueueConnection, long, ICreationScope> verify,
            Action<QueueConnection, ICreationScope> setErrorFlag,
            IGetTimeFactory timeFactory, ICreationScope scope,
            ILogger logProvider)
            where TTransportInit : ITransportInit, new()
            where TJobQueueCreator : class, IJobQueueCreation
        {
            _timeFactory = timeFactory;
            _logProvider = logProvider;
            _scope = scope;
            RunEnqueueTest<TTransportInit>(queueConnection, addInterceptors, verify,
                setErrorFlag,
                (x, name) => x.AddUpdateJob<TTransportInit, TJobQueueCreator>(name, queueConnection,
                    "min(*)",
                    (message, workerNotification) => Console.WriteLine(message.MessageId.Id.Value), null, config => { }),

                (x, name, time) => x.AddUpdateJob<TTransportInit, TJobQueueCreator>(name, queueConnection,
                    "min(*)",
                    (message, workerNotification) => Console.WriteLine(message.MessageId.Id.Value), null, config => { }, true, time), timeFactory, scope, logProvider

                );
        }

#if NETFULL
        public void RunEnqueueTestDynamic<TTransportInit, TJobQueueCreator>(QueueConnection queueConnection,
            bool addInterceptors,
            Action<QueueConnection, long, ICreationScope> verify,
            Action<QueueConnection, ICreationScope> setErrorFlag,
            IGetTimeFactory timeFactory, ICreationScope scope,
            ILogger logProvider)
            where TTransportInit : ITransportInit, new()
            where TJobQueueCreator : class, IJobQueueCreation
        {
            _timeFactory = timeFactory;
            _logProvider = logProvider;
            _scope = scope;
            using (var jobQueueCreation =
                new JobQueueCreationContainer<TTransportInit>(x => x.RegisterNonScopedSingleton(scope)))
            {
                using (
                    var createQueue = jobQueueCreation.GetQueueCreation<TJobQueueCreator>(queueConnection)
                    )
                {
                    RunEnqueueTest<TTransportInit>(queueConnection, addInterceptors, verify,
                        setErrorFlag,
                        (x, name) => x.AddUpdateJob<TTransportInit>(createQueue, name, queueConnection,
                            "min(*)",
                            new LinqExpressionToRun(
                                "(message, workerNotification) => Console.WriteLine(DateTime.Now.Ticks)")),

                        (x, name, time) =>
                            x.AddUpdateJob<TTransportInit>(createQueue, name, queueConnection,
                                "min(*)",
                                new LinqExpressionToRun(
                                    "(message, workerNotification) => Console.WriteLine(DateTime.Now.Ticks)"), null, null, true,
                                time), timeFactory, scope, logProvider
                        );
                }
            }
        }
#endif

        public
            void RunTestMultipleProducers<TTransportInit, TJobQueueCreator>(QueueConnection queueConnection,
                bool addInterceptors,
                long producerCount,
                IGetTimeFactory timeFactory,
                ILogger logProvider,
                ICreationScope scope)
            where TTransportInit : ITransportInit, new()
            where TJobQueueCreator : class, IJobQueueCreation
        {
            var enqueued = 0;
            Exception lastError = null;
            _queueStarted = false;
            _timeFactory = timeFactory;
            _logProvider = logProvider;
            _scope = scope;

            using (var jobQueueCreation =
                new JobQueueCreationContainer<TTransportInit>(x => x.RegisterNonScopedSingleton(scope)))
            {
                using (
                    var createQueue = jobQueueCreation.GetQueueCreation<TJobQueueCreator>(queueConnection)
                )
                {
                    createQueue.CreateJobSchedulerQueue(x => x.RegisterNonScopedSingleton(scope), queueConnection);

                    //always run a consumer to clear out jobs
                    using (var queueContainer = new QueueContainer<TTransportInit>((x) => QueueContainer(x, scope)))
                    {
                        using (var queue = queueContainer.CreateMethodConsumer(queueConnection, x => x.RegisterNonScopedSingleton(scope)))
                        {
                            queue.Configuration.Worker.WorkerCount = 4;
                            WaitForRollover(timeFactory);

                            Thread.Sleep(10000);

                            Parallel.For(0, producerCount, (i, loopState) =>
                            {
                                using (var jobContainer =
                                    new JobSchedulerContainer(x => x.RegisterNonScopedSingleton(scope)))
                                {
                                    using (var scheduler = CreateScheduler(jobContainer, addInterceptors, scope))
                                    {
                                        scheduler.OnJobQueue +=
                                            (job, message) => Interlocked.Increment(ref enqueued);
                                        scheduler.OnJobQueueException +=
                                            (job, exception) => lastError = exception;
                                        scheduler.Start();

                                        scheduler.AddUpdateJob<TTransportInit, TJobQueueCreator>(Job1, queueConnection,
                                            "min(*)",
                                            (message, workerNotification) => Console.Write(""));

                                        scheduler.AddUpdateJob<TTransportInit, TJobQueueCreator>(Job2, queueConnection,
                                            "min(*)",
                                            (message, workerNotification) =>
                                                Console.Write(""));

                                        WaitForRollover(timeFactory);
                                        StartConsumer(queue);
                                        WaitForEnQueue();
                                    }
                                }
                            });
                            ValidateEnqueueMultipleProducer(enqueued, lastError, 2);
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

        private void RunEnqueueTest<TTransportInit>(QueueConnection queueConnection,
            bool addInterceptors,
            Action<QueueConnection, long, ICreationScope> verify,
            Action<QueueConnection, ICreationScope> setErrorFlag,
            Func<IJobScheduler, string, IScheduledJob> enqueue,
            Func<IJobScheduler, string, TimeSpan, IScheduledJob> enqueueWindow,
            IGetTimeFactory timeFactory, ICreationScope scope,
            ILogger logProvider)
            where TTransportInit : ITransportInit, new()
        {
            _timeFactory = timeFactory;
            _logProvider = logProvider;
            _scope = scope;

            using (var jobContainer = new JobSchedulerContainer(RegisterService))
            {
                using (var scheduler = CreateScheduler(jobContainer, addInterceptors, scope))
                {
                    var enqueued = 0;
                    var nonFatal = 0;
                    Exception lastError = null;
                    // ReSharper disable once AccessToModifiedClosure
                    scheduler.OnJobQueue += (job, message) => enqueued++;
                    scheduler.OnJobQueueException += (job, exception) => lastError = exception;
                    // ReSharper disable once AccessToModifiedClosure
                    scheduler.OnJobNonFatalFailureQueue += (job, message) => nonFatal++;
                    scheduler.Start();

                    WaitForRollover(timeFactory);

                    var job1 = enqueue(scheduler, Job1);
                    var job2 = enqueue(scheduler, Job2); //job2 won't be referenced again, but ensures that we have multiple records in the queue for the first test

                    WaitForEnQueue();

                    ValidateEnqueue(queueConnection, verify, enqueued, lastError, nonFatal, 2, scope);

                    enqueued = 0;

                    //remove job2 from schedule - doesn't remove already queued work
                    job2.StopSchedule();
                    scheduler.RemoveJob(Job2);

                    WaitForRollover(timeFactory);

                    WaitForEnQueue();

                    //validate job1 is not queued a second time. There will still be 2 jobs in the transport storage (job1, job2)
                    ValidateNonFatalError(queueConnection, verify, enqueued, lastError, nonFatal, 2, scope);

                    RunConsumer<TTransportInit>(queueConnection, scope);
                    verify(queueConnection, 0, scope);

                    enqueued = 0;
                    nonFatal = 0;
                    WaitForRollover(timeFactory);

                    WaitForEnQueue();

                    ValidateEnqueue(queueConnection, verify, enqueued, lastError, nonFatal, 1, scope);

                    //validate that errors are replaced
                    setErrorFlag(queueConnection, scope);
                    enqueued = 0;
                    nonFatal = 0;
                    WaitForRollover(timeFactory);
                    WaitForEnQueue();
                    ValidateEnqueue(queueConnection, verify, enqueued, lastError, nonFatal, 1, scope);

                    RunConsumer<TTransportInit>(queueConnection, scope);
                    verify(queueConnection, 0, scope);

                    enqueued = 0;
                    nonFatal = 0;
                    job1.StopSchedule();
                    scheduler.RemoveJob(Job1);
                    WaitForRollover(timeFactory);
                    WaitForEnQueue(); //nothing will be queued, make sure we are past fire time
                    enqueueWindow(scheduler, Job1, TimeSpan.FromSeconds(40)); //should be fired right away, since we are inside the window
                    Thread.Sleep(5000);
                    ValidateEnqueue(queueConnection, verify, enqueued, lastError, nonFatal, 1, scope);
                }
            }
        }

        private void RegisterService(IContainer container)
        {
            container.Register(() => _timeFactory, LifeStyles.Singleton);
            container.RegisterNonScopedSingleton(_scope);
        }

        private void WaitForEnQueue()
        {
            Thread.Sleep(20000);
        }
        private void WaitForRollover(IGetTimeFactory timeFactory)
        {
            var getTime = timeFactory.Create();
            while (getTime.GetCurrentUtcDate().Second != 55)
            {
                Thread.Sleep(100);
            }
        }
        private void ValidateEnqueue(QueueConnection queueConnection, Action<QueueConnection, long, ICreationScope> verify,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            long enqueued,
            Exception error,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            long nonFatal,
            long expectedEnqueue, ICreationScope scope)
        {
            if (error != null)
            {
                throw new Exception("Fatal error!", error);
            }
            Assert.Equal(expectedEnqueue, enqueued);
            Assert.Equal(0, nonFatal);
            verify(queueConnection, expectedEnqueue, scope);
        }
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void ValidateEnqueueMultipleProducer(long enqueued, Exception error,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            long expectedEnqueue)
        {
            if (error != null)
            {
                throw new Exception("Fatal error!", error);
            }
            Assert.Equal(expectedEnqueue, enqueued);
        }
        private void ValidateNonFatalError(QueueConnection queueConnection, Action<QueueConnection, long, ICreationScope> verify,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            long enqueued, Exception error,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            long nonFatal, long inQueueCount, ICreationScope scope)
        {
            Assert.Equal(0, enqueued);
            error.Should().BeNull("no errors should occur");
            Assert.Equal(1, nonFatal);
            verify(queueConnection, inQueueCount, scope);
        }
        private void RunConsumer<TTransportInit>(QueueConnection queueConnection, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
        {
            {
                using (var queueContainer = new QueueContainer<TTransportInit>((x) => QueueContainer(x, scope)))
                {
                    using (var queue = queueContainer.CreateMethodConsumer(queueConnection, x => x.RegisterNonScopedSingleton(scope)))
                    {
                        queue.Configuration.Worker.WorkerCount = 1;
                        queue.Start();
                        Thread.Sleep(9500);
                    }
                }
            }
        }

        private IJobScheduler CreateScheduler(JobSchedulerContainer container, bool addInterceptors, ICreationScope scope)
        {
            if (!addInterceptors)
            {
                return container.CreateJobScheduler(y => y.RegisterNonScopedSingleton(scope), y => y.RegisterNonScopedSingleton(scope),
                    z => { });
            }
            return container.CreateJobScheduler(((x) => QueueContainer(x, scope)), y => y.RegisterNonScopedSingleton(scope),
                z => { });
        }

        private void QueueContainer(IContainer container, ICreationScope scope)
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

            container.Register(() => _logProvider, LifeStyles.Singleton);
            container.RegisterNonScopedSingleton(scope);
        }
    }
}
